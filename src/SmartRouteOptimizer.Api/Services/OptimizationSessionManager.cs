using SmartRouteOptimizer.Api.Models;

namespace SmartRouteOptimizer.Api.Services
{
    public class OptimizationSessionManager
    {
        private readonly Dictionary<Guid, OptimizationSession> _sessions = new();

        public OptimizationSession CreateSession(OptimizationRequest request)
        {
            var session = new OptimizationSession(request);
            _sessions[session.Id] = session;
            return session;
        }

        public OptimizationSession? GetSession(Guid id)
        {
            return _sessions.TryGetValue(id, out var session) ? session : null;
        }
    }
}
