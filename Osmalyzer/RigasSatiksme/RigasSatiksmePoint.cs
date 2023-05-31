namespace Osmalyzer
{
    public class RigasSatiksmePoint
    {
        public RigasSatiksmeTrip Trip { get; }
        
        public RigasSatiksmeStop Stop { get; }


        public RigasSatiksmePoint(RigasSatiksmeTrip trip, RigasSatiksmeStop stop)
        {
            Trip = trip;
            Stop = stop;
        }
    }
}