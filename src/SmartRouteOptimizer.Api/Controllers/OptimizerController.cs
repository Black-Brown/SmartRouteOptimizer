using SmartRouteOptimizer.Api.Models;
using SmartRouteOptimizer.Api.Services;
using Microsoft.AspNetCore.Mvc;


namespace LastMileOptimizer.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class OptimizerController : ControllerBase
{
    private readonly OptimizationEngine _engine;


    public OptimizerController(OptimizationEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Inicia una optimización paralela. 
    /// Devuelve un sessionId para consultar progreso/resultados.
    /// </summary>
    [HttpPost("start")]
    public ActionResult<object> Start([FromBody] OptimizationRequest request)
    {
        if (request is null || request.Vehicles is null || request.Clients is null)
            return BadRequest("Request inválido");


        var sessionId = _engine.StartOptimization(request);
        return Ok(new { sessionId });
    }


    /// <summary>
    /// Progreso de una sesión (métricas en tiempo real y el estado de cada algoritmo).
    /// </summary>
    [HttpGet("status/{sessionId}")]
    public ActionResult<ProgressDto> Status([FromRoute] Guid sessionId)
    {
        var status = _engine.GetProgress(sessionId);
        if (status is null) return NotFound();
        return Ok(status);
    }


    /// <summary>
    /// Resultado final de la optimización. 404 por si aún no terminó.
    /// </summary>
    [HttpGet("result/{sessionId}")]
    public ActionResult<SolutionDto> Result([FromRoute] Guid sessionId)
    {
        var solution = _engine.GetSolution(sessionId);
        if (solution is null) return NotFound();
        return Ok(solution);
    }
}