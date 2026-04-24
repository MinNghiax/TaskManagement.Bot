#!/usr/bin/env node


'use strict';
const fs   = require('fs');
const path = require('path');

const [,, inputFile, outputFile, protoPackage = 'mezon', csharpNamespace = '', crossPackage = '', excludeProtoFile = ''] = process.argv;

if (!inputFile || !outputFile) {
  console.error('Usage: node reconstruct-proto.js <input.ts> <output.proto> [package] [csharp_namespace]');
  process.exit(1);
}

const src = fs.readFileSync(inputFile, 'utf8');

function parseImportAliases(src) {
  const aliases = new Map();
  const importBlockRe = /^import\s*\{([^}]+)\}\s*from\s*["'][^"']+["']/gm;
  let m;
  while ((m = importBlockRe.exec(src)) !== null) {
    const body = m[1];
    const itemRe = /(\w+)\s+as\s+(\w+)/g;
    let im;
    while ((im = itemRe.exec(body)) !== null) {
      aliases.set(im[2], im[1]); 
    }
  }
  return aliases;
}

const importAliases = parseImportAliases(src);



const wireTypeHint = { 0: 'varint', 1: 'fixed64', 2: 'len', 5: 'fixed32' };


const writerMethodToProto = {
  string: 'string',
  int32:  'int32',
  int64:  'int64',
  uint32: 'uint32',
  uint64: 'uint64',
  sint32: 'sint32',
  sint64: 'sint64',
  bool:   'bool',
  bytes:  'bytes',
  float:  'float',
  double: 'double',
  fixed32:'fixed32',
  fixed64:'fixed64',
};


function decodeTag(tag) {
  return { fieldNumber: tag >>> 3, wireType: tag & 0x7 };
}



function parseInterfaces(src) {
  const result = new Map();
  const ifaceRe = /^export interface (\w+)\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}/gm;
  let m;
  while ((m = ifaceRe.exec(src)) !== null) {
    const name   = m[1];
    const body   = m[2];
    const fields = new Map();
    const fieldRe = /^\s{2,}(\w+)\??:\s*([^;]+);/gm;
    let fm;
    while ((fm = fieldRe.exec(body)) !== null) {
      fields.set(fm[1], fm[2].trim());
    }
    result.set(name, fields);
  }
  return result;
}



function parseEncodes(src, interfaces) {
  const result = new Map();

  const constRe = /export const (\w+) = \{[^]*?encode\(\w+:\s*\1[^)]*\)[^{]*\{/g;
  let cm;
  while ((cm = constRe.exec(src)) !== null) {
    const name    = cm[1];
    const start   = cm.index + cm[0].length;

    let depth = 1;
    let i     = start;
    while (i < src.length && depth > 0) {
      if (src[i] === '{') depth++;
      else if (src[i] === '}') depth--;
      i++;
    }
    const encodeBody = src.slice(start, i - 1);

    const fields = parseEncodeBody(encodeBody, name, interfaces);
    result.set(name, fields);
  }
  return result;
}


function resolveType(typeName) {
  return importAliases.get(typeName) || typeName;
}


function parseEncodeBody(body, messageName, interfaces) {
  const fields     = [];
  const seen       = new Set();   

  const ifaceFields = interfaces.get(messageName) || new Map();

  const scalarRe = /writer\.uint32\((\d+)\)\.(string|int32|int64|uint32|uint64|sint32|sint64|bool|bytes|float|double|fixed32|fixed64)\(message\.(\w+)\)/g;
  let m;
  while ((m = scalarRe.exec(body)) !== null) {
    const tag         = parseInt(m[1], 10);
    const { fieldNumber } = decodeTag(tag);
    const method      = m[2];
    const fieldName   = m[3];
    if (!seen.has(fieldNumber)) {
      seen.add(fieldNumber);
      fields.push({ fieldNumber, fieldName, protoType: writerMethodToProto[method], repeated: false });
    }
  }

  const msgRe = /(\w+)\.encode\(message\.(\w+),\s*writer\.uint32\((\d+)\)\.fork\(\)\)\.ldelim\(\)/g;
  while ((m = msgRe.exec(body)) !== null) {
    const typeName  = resolveType(m[1]);
    const fieldName = m[2];
    const tag       = parseInt(m[3], 10);
    const { fieldNumber } = decodeTag(tag);
    if (!seen.has(fieldNumber)) {
      seen.add(fieldNumber);
      fields.push({ fieldNumber, fieldName, protoType: typeName, repeated: false });
    }
  }

  const repMsgRe = /for\s*\(const v of message\.(\w+)\)\s*\{[^}]*?(\w+)\.encode\(v!?,\s*writer\.uint32\((\d+)\)\.fork\(\)\)\.ldelim\(\)/g;
  while ((m = repMsgRe.exec(body)) !== null) {
    const fieldName = m[1];
    const typeName  = resolveType(m[2]);
    const tag       = parseInt(m[3], 10);
    const { fieldNumber } = decodeTag(tag);
    if (!seen.has(fieldNumber)) {
      seen.add(fieldNumber);
      fields.push({ fieldNumber, fieldName, protoType: typeName, repeated: true });
    }
  }

  const repScalarRe = /for\s*\(const v of message\.(\w+)\)\s*\{[^}]*?writer\.uint32\((\d+)\)\.(string|int32|int64|uint32|uint64|bool|bytes|float|double)\(v!?\)/g;
  while ((m = repScalarRe.exec(body)) !== null) {
    const fieldName = m[1];
    const tag       = parseInt(m[2], 10);
    const { fieldNumber } = decodeTag(tag);
    const method    = m[3];
    if (!seen.has(fieldNumber)) {
      seen.add(fieldNumber);
      fields.push({ fieldNumber, fieldName, protoType: writerMethodToProto[method], repeated: true });
    }
  }

  const wrapperMap = {
    StringValue: 'string', BoolValue: 'bool', Int32Value: 'int32', Int64Value: 'int64',
    UInt32Value: 'uint32', UInt64Value: 'uint64', FloatValue: 'float', DoubleValue: 'double',
    BytesValue: 'bytes',
  };
  const wrapperRe = /(\w+Value)\.encode\(\{\s*value:\s*(?:message\.(\w+)|message\["(\w+)"\])!?\s*\},\s*writer\.uint32\((\d+)\)\.fork\(\)\)\.ldelim\(\)/g;
  while ((m = wrapperRe.exec(body)) !== null) {
    const wrapperType = m[1];
    const fieldName   = m[2] || m[3];
    const tag         = parseInt(m[4], 10);
    const { fieldNumber } = decodeTag(tag);
    const protoType   = wrapperMap[wrapperType] || wrapperType;
    if (!seen.has(fieldNumber)) {
      seen.add(fieldNumber);
      fields.push({ fieldNumber, fieldName, protoType, repeated: false });
    }
  }

  const packedRe = /writer\.uint32\((\d+)\)\.fork\(\);\s*for\s*\(const v of message\.(\w+)\)\s*\{\s*writer\.(int64|int32|uint32|uint64|sint32|sint64|bool)\(v\);\s*\}/g;
  while ((m = packedRe.exec(body)) !== null) {
    const tag       = parseInt(m[1], 10);
    const { fieldNumber } = decodeTag(tag);
    const fieldName = m[2];
    const method    = m[3];
    if (!seen.has(fieldNumber)) {
      seen.add(fieldNumber);
      fields.push({ fieldNumber, fieldName, protoType: writerMethodToProto[method], repeated: true });
    }
  }

  return fields.sort((a, b) => a.fieldNumber - b.fieldNumber);
}



const GOOGLE_WRAPPERS  = new Set(['BoolValue', 'Int32Value', 'StringValue', 'DoubleValue', 'FloatValue', 'Int64Value', 'UInt32Value', 'UInt64Value', 'BytesValue']);
const PROTO_SCALARS    = new Set(Object.values(writerMethodToProto));

function collectImports(encodeMap, localNames, crossFileTypes) {
  const imports = new Set();
  for (const fields of encodeMap.values()) {
    for (const f of fields) {
      if (!PROTO_SCALARS.has(f.protoType) && !localNames.has(f.protoType)) {
        if (GOOGLE_WRAPPERS.has(f.protoType)) {
          imports.add('google');
        } else if (crossFileTypes.has(f.protoType)) {
          imports.add('cross');
        }
      }
    }
  }
  return imports;
}


function emitProto(encodeMap, interfacesMap, protoPackage, csharpNamespace, extraImports) {
  const lines = [];

  lines.push('syntax = "proto3";');
  lines.push('');
  lines.push(`package ${protoPackage};`);
  if (csharpNamespace) {
    lines.push(`option csharp_namespace = "${csharpNamespace}";`);
  }
  lines.push('');

  for (const imp of extraImports) {
    if (imp === 'google') {
      lines.push('import "google/protobuf/wrappers.proto";');
    } else {
      lines.push(`import "${imp}";`);
    }
  }
  if (extraImports.size > 0) lines.push('');

  for (const [msgName, fields] of encodeMap) {
    lines.push(`message ${msgName} {`);
    for (const f of fields) {
      const repeated = f.repeated ? 'repeated ' : '';
      lines.push(`  ${repeated}${f.protoType} ${f.fieldName} = ${f.fieldNumber};`);
    }
    lines.push('}');
    lines.push('');
  }

  return lines.join('\n');
}


function parseProtoMessageNames(protoPath) {
  const names = new Set();
  if (!protoPath || !fs.existsSync(protoPath)) return names;
  const text = fs.readFileSync(protoPath, 'utf8');
  const re = /^message\s+(\w+)\s*\{/gm;
  let m;
  while ((m = re.exec(text)) !== null) {
    names.add(m[1]);
  }
  return names;
}

const excludedNames = parseProtoMessageNames(excludeProtoFile);
if (excludedNames.size > 0) {
  console.log(`Excluding ${excludedNames.size} messages already defined in ${excludeProtoFile}`);
}

console.log(`Reading ${inputFile}…`);
const interfaces = parseInterfaces(src);
const encodeMap  = parseEncodes(src, interfaces);

for (const name of excludedNames) {
  encodeMap.delete(name);
}

console.log(`Found ${encodeMap.size} messages (after exclusions).`);

const localNames = new Set(encodeMap.keys());

const extraImports = new Set();

if (crossPackage) {
  for (const [msgName, fields] of encodeMap) {
    for (const f of fields) {
      if (!PROTO_SCALARS.has(f.protoType) && !GOOGLE_WRAPPERS.has(f.protoType) && !localNames.has(f.protoType)) {
        f.protoType = `${crossPackage}.${f.protoType}`;
      }
    }
  }
}

if (src.includes('from "../google/protobuf/wrappers"') || src.includes('from "./google/protobuf/wrappers"')) {
  for (const fields of encodeMap.values()) {
    if (fields.some(f => GOOGLE_WRAPPERS.has(f.protoType))) {
      extraImports.add('google');
      break;
    }
  }
}

const crossTypes = new Set();
for (const fields of encodeMap.values()) {
  for (const f of fields) {
    if (!PROTO_SCALARS.has(f.protoType) && !GOOGLE_WRAPPERS.has(f.protoType) && !localNames.has(f.protoType)) {
      crossTypes.add(f.protoType);
    }
  }
}

if (crossTypes.size > 0) {
  console.log(`Cross-file type references: ${[...crossTypes].join(', ')}`);
  extraImports.add('api.proto');
}

const protoText = emitProto(encodeMap, interfaces, protoPackage, csharpNamespace, extraImports);

const outDir = path.dirname(outputFile);
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });

fs.writeFileSync(outputFile, protoText, 'utf8');
console.log(`Written to ${outputFile}`);
console.log(`Messages: ${[...encodeMap.keys()].join(', ')}`);
