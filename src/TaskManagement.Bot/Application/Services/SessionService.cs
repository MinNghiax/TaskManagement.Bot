using System;
using System.Collections.Generic;
using TaskManagement.Bot.Application.Sessions;

namespace TaskManagement.Bot.Application.Services
{
    public class SessionService
    {
        // 🔥 dùng SenderId thay vì Username
        private static Dictionary<long, UserSession> _sessions = new();

        public UserSession? Get(long userId)
            => _sessions.ContainsKey(userId) ? _sessions[userId] : null;

        public void Set(long userId, UserSession session)
            => _sessions[userId] = session;

        public void Remove(long userId)
            => _sessions.Remove(userId);
    }
}