package ch.idsia.ai.agents;

public class Sensor
{
    private int xDistance;
    private int yDistance;
    private byte type;
    private float totalDistance;

    public Sensor(int t, int dx, int dy)
    {
        type = (byte)t;
        xDistance = dx;
        yDistance = dy;

        totalDistance = (float)Math.sqrt(xDistance * xDistance + yDistance * yDistance);
    }

    public int getXDistance(){ return xDistance; }
    public int getYDistance(){ return yDistance; }
    public byte getType() { return type; }
    public float getTotalDistance() { return totalDistance; }
}
