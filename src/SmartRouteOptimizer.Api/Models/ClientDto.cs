namespace SmartRouteOptimizer.Api.Models
{
    public class ClientDto
    {
        public int Id { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int Prioridad { get; set; }
        public double VentanaInicio { get; set; }
        public double VentanaFin { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
