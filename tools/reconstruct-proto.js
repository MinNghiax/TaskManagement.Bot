#!/usr/bin/env node
/**
 * reconstruct-proto.js
 *
 * Reconstructs .proto files from ts-proto-generated TypeScript files.
 *
 * Usage:
 *   node reconstruct-proto.js <input.ts> <output.proto> [package] [csharp_namespace]
 *
 * Examples:
 *   node reconstruct-proto.js ../mezon-js/packages/mezon-js-protobuf/api/api.ts     out/api.proto     "mezon.api"      "Mezon.Sdk.Proto"
 *   node reconstruct-proto.js ../mezon-js/packages/mezon-js-protobuf/rtapi/realtime.ts out/realtime.proto "mezon.realtime" "Mezon.Sdk.Proto"
 */

'use strict';
const fs   = require('fs');
const path = require('path');

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------
// Optional env var: CROSS_PACKAGE=mezon.api  → prefix cross-file types with that package
// Optional: path to an already-generated proto file whose messages should be excluded
// (to avoid "already defined" errors when the same message appears in two TS files)
const [,, inputFile, outputFile, protoPackage = 'mezon', csharpNamespace = '', crossPackage = '', excludeProtoFile = ''] = process.argv;

if (!inputFile || !outputFile) {
  console.error('Usage: node reconstruct-proto.js <input.ts> <output.proto> [package] [csharp_namespace]');
  process.exit(1);
}

const src = fs.readFileSync(inputFile, 'utf8');

// ---------------------------------------------------------------------------
// Step 0: parse import aliases  e.g.  import { Foo as Bar } from "..."
// Returns Map<tsAlias, originalName>
// ---------------------------------------------------------------------------
function parseImportAliases(src) {
  const aliases = new Map();
  // Match entire import blocks: import { ... } from "..."
  const importBlockRe = /^import\s*\{([^}]+)\}\s*from\s*["'][^"']+["']/gm;
  let m;
  while ((m = importBlockRe.exec(src)) !== null) {
    const body = m[1];
    // Each item:  Name as Alias  or just  Name
    const itemRe = /(\w+)\s+as\s+(\w+)/g;
    let im;
    while ((im = itemRe.exec(body)) !== null) {
      aliases.set(im[2], im[1]); // alias → original
    }
  }
  return aliases;
}

const importAliases = parseImportAliases(src);

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** wire_type → proto scalar type hint (we refine below) */
const wireTypeHint = { 0: 'varint', 1: 'fixed64', 2: 'len', 5: 'fixed32' };

/** Map from .writer method name → proto type */
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

/**
 * Decode a protobuf tag back to { fieldNumber, wireType }
 */
function decodeTag(tag) {
  return { fieldNumber: tag >>> 3, wireType: tag & 0x7 };
}

// ---------------------------------------------------------------------------
// Step 1: collect all interface definitions  (field name → TS type)
// ---------------------------------------------------------------------------

/**
 * Returns a map: messageName → Map<fieldName, tsType>
 * We only look at "export interface Foo {" blocks.
 */
function parseInterfaces(src) {
  const result = new Map();
  // Match top-level export interface blocks
  const ifaceRe = /^export interface (\w+)\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}/gm;
  let m;
  while ((m = ifaceRe.exec(src)) !== null) {
    const name   = m[1];
    const body   = m[2];
    const fields = new Map();
    // Each field line:  fieldName?: Type | undefined; or fieldName: Type;
    const fieldRe = /^\s{2,}(\w+)\??:\s*([^;]+);/gm;
    let fm;
    while ((fm = fieldRe.exec(body)) !== null) {
      fields.set(fm[1], fm[2].trim());
    }
    result.set(name, fields);
  }
  return result;
}

// ---------------------------------------------------------------------------
// Step 2: parse every encode() function
// ---------------------------------------------------------------------------

/**
 * Returns:  messageName → [ { fieldNumber, fieldName, protoType, repeated } ]
 */
function parseEncodes(src, interfaces) {
  const result = new Map();

  // Find every "export const Foo = {" block, then pull out the encode function
  // We need the text between "encode(...) {" and the first unindented "},"
  // Note: ts-proto uses several param name conventions:
  //   encode(message: TypeName, ...)   — standard
  //   encode(_message: TypeName, ...)  — unused param convention
  //   encode(_: TypeName, ...)         — fully unnamed param (truly empty messages)
  const constRe = /export const (\w+) = \{[^]*?encode\(\w+:\s*\1[^)]*\)[^{]*\{/g;
  let cm;
  while ((cm = constRe.exec(src)) !== null) {
    const name    = cm[1];
    const start   = cm.index + cm[0].length;

    // Walk forward to find the closing brace of the encode function
    let depth = 1;
    let i     = start;
    while (i < src.length && depth > 0) {
      if (src[i] === '{') depth++;
      else if (src[i] === '}') depth--;
      i++;
    }
    const encodeBody = src.slice(start, i - 1);

    const fields = parseEncodeBody(encodeBody, name, interfaces);
    // Always include, even empty messages (valid in proto3: message Ping {})
    result.set(name, fields);
  }
  return result;
}

/** Resolve a TS type name through import aliases to its original proto name */
function resolveType(typeName) {
  return importAliases.get(typeName) || typeName;
}

/**
 * Given the body text of an encode() function, return field descriptors.
 */
function parseEncodeBody(body, messageName, interfaces) {
  const fields     = [];
  const seen       = new Set();   // field numbers already recorded

  const ifaceFields = interfaces.get(messageName) || new Map();

  // -------------------------------------------------------------------------
  // Pattern A: scalar field
  //   writer.uint32(TAG).method(message.fieldName)
  // -------------------------------------------------------------------------
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

  // -------------------------------------------------------------------------
  // Pattern B: embedded message (non-repeated)
  //   TypeName.encode(message.fieldName, writer.uint32(TAG).fork()).ldelim()
  // -------------------------------------------------------------------------
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

  // -------------------------------------------------------------------------
  // Pattern C: repeated embedded message
  //   for (const v of message.fieldName) {
  //     TypeName.encode(v!, writer.uint32(TAG).fork()).ldelim();
  // -------------------------------------------------------------------------
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

  // -------------------------------------------------------------------------
  // Pattern D: repeated scalar (non-packed)
  //   for (const v of message.fieldName) {
  //     writer.uint32(TAG).method(v!?)
  // -------------------------------------------------------------------------
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

  // -------------------------------------------------------------------------
  // Pattern E2: google wrapper type field (optional scalar via wrapper message)
  //   StringValue.encode({ value: message.fieldName }, writer.uint32(TAG).fork()).ldelim()
  //   → optional string fieldName
  // The wrapper type (e.g. StringValue) maps to the inner scalar type in proto3 optional.
  // We emit as the scalar type (string, bool, int32, etc.) since proto3 wrapper = optional field.
  // -------------------------------------------------------------------------
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
      // Google wrapper types → optional scalar (represented as plain scalar in proto3)
      fields.push({ fieldNumber, fieldName, protoType, repeated: false });
    }
  }

  // -------------------------------------------------------------------------
  // Pattern E: packed repeated scalar (fork/loop/ldelim)
  //   writer.uint32(TAG).fork();
  //   for (const v of message.fieldName) {
  //     writer.method(v);
  //   }
  //   writer.ldelim();
  // -------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Step 3: determine which proto types are external imports
// ---------------------------------------------------------------------------

/**
 * Given the message map and the list of known local message names,
 * returns the set of type names that look like imports from another proto file.
 *
 * We detect "BoolValue", "Int32Value", "StringValue" as google.protobuf wrappers,
 * everything else that is a message type (not a scalar) and not defined locally
 * is flagged for import.
 */
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

// ---------------------------------------------------------------------------
// Step 4: emit .proto text
// ---------------------------------------------------------------------------

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

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Step 0b: parse message names from an existing proto file (for exclusion)
// ---------------------------------------------------------------------------
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

// Remove messages that are already defined in the exclude file
for (const name of excludedNames) {
  encodeMap.delete(name);
}

console.log(`Found ${encodeMap.size} messages (after exclusions).`);

// Determine cross-file imports: any type referenced in this file but not defined here
const localNames = new Set(encodeMap.keys());

// For well-known extra imports, look for the "import" statements at top of TS file
const extraImports = new Set();

// If a cross-package prefix is given, rewrite cross-file type references in the encode map
if (crossPackage) {
  for (const [msgName, fields] of encodeMap) {
    for (const f of fields) {
      if (!PROTO_SCALARS.has(f.protoType) && !GOOGLE_WRAPPERS.has(f.protoType) && !localNames.has(f.protoType)) {
        f.protoType = `${crossPackage}.${f.protoType}`;
      }
    }
  }
}

// Check if this file imports from google wrappers
if (src.includes('from "../google/protobuf/wrappers"') || src.includes('from "./google/protobuf/wrappers"')) {
  // Check if any field uses a wrapper type
  for (const fields of encodeMap.values()) {
    if (fields.some(f => GOOGLE_WRAPPERS.has(f.protoType))) {
      extraImports.add('google');
      break;
    }
  }
}

// For the realtime.proto, we need to import api.proto for cross-file types
// Detect by checking if any message type is NOT defined locally AND not a scalar/wrapper
// Types in excludedNames come from the other proto file (same package), so they don't need a separate import,
// but we do need to import the file itself if we reference any of them.
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

// Create output directory if needed
const outDir = path.dirname(outputFile);
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });

fs.writeFileSync(outputFile, protoText, 'utf8');
console.log(`Written to ${outputFile}`);
console.log(`Messages: ${[...encodeMap.keys()].join(', ')}`);
