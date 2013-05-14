namespace DrawSmoother
{
    public class Particle
    {
        public float x, y;
        
        public float x1, y1, x2, y2;
      
        public Particle() { }

        public Particle(float _x, float _y)
        {
            x = _x;
            y = _y;
            x1 = y1 = x2 = y2 = -1f;
        }
    }

}
