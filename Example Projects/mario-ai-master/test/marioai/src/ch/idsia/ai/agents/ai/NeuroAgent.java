package ch.idsia.ai.agents.ai;

import ch.idsia.ai.agents.Agent;
import ch.idsia.ai.agents.Sensor;
import ch.idsia.mario.environments.Environment;
import ch.idsia.mario.engine.sprites.Mario;
import ch.idsia.mario.engine.sprites.Sprite;
import ch.idsia.mario.engine.LevelScene;
import ch.idsia.mario.engine.GlobalOptions;
import ch.idsia.tools.tcp.Server;

import java.awt.event.KeyAdapter;
import java.awt.event.KeyEvent;
import java.awt.*;
import java.io.Console;
import java.util.ArrayList;

/**
 * Created by IntelliJ IDEA.
 * User: Sergey Karakovskiy
 * Date: Apr 25, 2009
 * Time: 12:30:41 AM
 * Package: ch.idsia.ai.agents.ai;
 */
public class NeuroAgent extends KeyAdapter implements Agent
{
    public static final int SENSOR_COUNT_WALL = 1;
    public static final int SENSOR_SIZE_WALL = 2;
    
    public static final int SENSOR_COUNT_ENEMY = 3;
    public static final int SENSOR_SIZE_ENEMY = 2;

    public static final int SENSOR_COUNT_POWERUP = 2;
    public static final int SENSOR_SIZE_POWERUP = 2;

    public static final int SENSOR_COUNT_PIT = 1;
    public static final int SENSOR_SIZE_PIT = 2;

    public static final int SENSOR_COUNT_LUIGI = 3;
    public static final int SENSOR_SIZE_LUIGI = 1;

    public static final int TOTAL_SENSOR_BYTES =
              SENSOR_COUNT_WALL     * SENSOR_SIZE_WALL
            + SENSOR_COUNT_ENEMY    * SENSOR_SIZE_ENEMY
            + SENSOR_COUNT_POWERUP  * SENSOR_SIZE_POWERUP
            + SENSOR_COUNT_PIT      * SENSOR_SIZE_PIT
            + SENSOR_COUNT_LUIGI    * SENSOR_SIZE_LUIGI;

    protected byte[] sensorData = new byte[TOTAL_SENSOR_BYTES];
    protected boolean action[] = new boolean[Environment.numberOfButtons];
    protected String name = "NeuroAgent";
    protected Server server;

    protected static double oldDistance = 0;
    protected static double distanceTraveled = 0;
    protected static double timer = -1;
    
    public NeuroAgent(Server serverIn)
    {
        timer = -1;
        server = serverIn;
        this.reset();
    }

    public void reset()
    {
        action = new boolean[Environment.numberOfButtons];// Empty action
    }

    public static byte[] generateSensors(Environment observation, Graphics graphics)
    {
        byte[][] obs = observation.getLevelSceneObservation();

        boolean large = observation.getMarioMode() > 0;
        float[] marioPos = observation.getMarioFloatPos();
        float[] enemyPos = observation.getEnemiesFloatPos(1);


        if(timer < 0)
        {
            timer = 0;
            distanceTraveled = 0;
            oldDistance = marioPos[0];
        }
        else
        {
            double deltaDistance = marioPos[0] - oldDistance;
            oldDistance = marioPos[0];

            timer++;
            distanceTraveled += Math.abs(deltaDistance);

            if(timer >= 15 * 5)
            {
                if(distanceTraveled < 50)
                    observation.getMario().die(false);

                timer = 0;
                distanceTraveled = 0;
            }
        }


        Point marioPoint = observation.getMarioPixePos();
        Point cam = observation.getCamPoint();
        Point off1 = new Point(-cam.x + 16,- cam.y + 8 + (large?16:0));
        Point off2 = new Point(15 - ((int)marioPos[0] - 1 + (large?0:1)) % 16 - (large?0:6), 15 - ((int)marioPos[1] - 1) % 16 + 8);
        Point off3 = new Point(15 - ((int)marioPos[0] ) % 16 - 15, 15 - ((int)marioPos[1]  ) % 16 );
        Point origin = new Point( marioPoint.x + off1.x, marioPoint.y + off1.y);

        int wallDistance = 0;
        int wallHeight   = 0;

        for( int x=Environment.HalfObsWidth; x<Environment.HalfObsWidth*2; x++)
        {
            if(obs[Environment.HalfObsHeight][x] != 0)
            {
                wallDistance = 16 * (x - Environment.HalfObsWidth) + off3.x - 5;

                if(wallDistance < 0)
                    wallDistance = 0;

                int y = Environment.HalfObsHeight;

                while(y > 0 && obs[y][x] != 0)
                    y--;

                wallHeight = Environment.HalfObsHeight - y;
                
                break;
            }
        }

        Sensor[] enemySensors = new Sensor[SENSOR_COUNT_ENEMY];
        Sensor[] powerupSensors = new Sensor[SENSOR_COUNT_POWERUP];
        
        for(int i=0; i<enemySensors.length; i++)
            enemySensors[i] = new Sensor(0,Environment.HalfObsWidth * 16,Environment.HalfObsHeight * 16);

        for(int i=0; i<powerupSensors.length; i++)
            powerupSensors[i] = new Sensor(0,Environment.HalfObsWidth * 16,Environment.HalfObsHeight * 16);

        int powerupCount = 0;
        int maxXDistance = Environment.HalfObsWidth * 16;
        int maxYDistance = Environment.HalfObsHeight * 16;

        // get the closest enemies and powerups
        for(int i=0; i<enemyPos.length; i+=3)
        {
            Sensor sensor = new Sensor((byte)enemyPos[i], (int)(enemyPos[i+1] - marioPos[0]), (int)(enemyPos[i+2] - marioPos[1]));

            Sensor[] list;

            int startIndex, endIndex;

            if(sensor.getType() == Sprite.KIND_MUSHROOM || sensor.getType() == Sprite.KIND_FIRE_FLOWER)
            {
                list = powerupSensors;
                startIndex = list.length - 1;
                endIndex = 0;
                powerupCount++;
            }
            else
            {
                list = enemySensors;

                if(sensor.getType() == Sprite.KIND_SPIKY)
                {
                    startIndex = 0;
                    endIndex = 0;
                }
                else
                {
                    startIndex = list.length-1;
                    endIndex = 1;
                }
            }


            for(int j=startIndex; j>=endIndex; j--)
            {
                if(sensor.getTotalDistance() < list[j].getTotalDistance()
                        && Math.abs(sensor.getXDistance()) <= maxXDistance
                        && Math.abs(sensor.getYDistance()) <= maxYDistance)
                {
                    if(j + 1 <= startIndex)
                        list[j+1] = list[j];
                    list[j] = sensor;
                }
            }
        }

        // if the list of powerups isn't filled, then fill the rest with question blocks and bricks
        if(powerupCount < powerupSensors.length)
        {
            for(int y=0; y<Environment.HalfObsHeight*1; y++)
            {
                for(int x=0; x<Environment.HalfObsWidth*2; x++)
                {
                    byte e = obs[y][x];

                    // check for brick or question block
                    if(e == 16 || e == 21)
                    {
                        Sensor block = new Sensor(e, 16 * (x - Environment.HalfObsWidth) + off2.x - 8, 16 * (y - Environment.HalfObsHeight) + off2.y);

                        // sort blocks by type first, then distance
                        // i.e. powerups always have highest priority followed by question blocks then bricks
                        // if comparing two of the same type, take the one that is closest to player
                        for(int j=powerupSensors.length-1; j>=powerupCount && j>0; j--)
                        {
                            if(getBlockValue(block)  > getBlockValue(powerupSensors[j]) ||
                              (getBlockValue(block) == getBlockValue(powerupSensors[j]) && block.getTotalDistance() < powerupSensors[j].getTotalDistance() ) )
                            {
                                if(j + 1 < powerupSensors.length)
                                    powerupSensors[j+1] = powerupSensors[j];
                                powerupSensors[j] = block;
                            }
                        }
                    }
                }
            }
        }

        int pitDistance = 0;
        int pitWidth = 0;
        boolean foundPit = false;
        Point worldSize = observation.getWorldSize();

        for(int x=Environment.HalfObsWidth; x<Environment.HalfObsWidth*2; x++)
        {
            int maxY = Environment.HalfObsHeight*2-1;
            int y = (worldSize.y * 16 - marioPoint.y - (large?16:0) )  / 16 + Environment.HalfObsHeight - 1;

            if( y > maxY )
                y = maxY;

            if(obs[y][x] == 0)
            {
                if(!foundPit)
                {
                    foundPit = true;
                    pitDistance = 16 * (x - Environment.HalfObsWidth) + off3.x - (large?0:8);
                }
                
                pitWidth++;
            }
            else
            {
                if(foundPit)
                    break;
            }            
        }

        // Draw wall sensor
        if(wallHeight > 0)
        {
            LevelScene.drawLine(graphics, origin, new Point(wallDistance + 5, 0), true, Color.BLUE);
            LevelScene.drawLine(graphics, origin, new Point(wallDistance + 5, -wallHeight * 16 + off2.y), true, Color.BLUE);
        }

        // Draw enemy sensors
        for(int i=0; i<enemySensors.length; i++)
        {
            Sensor sensor = enemySensors[i];
            if(sensor.getType() != 0)
                LevelScene.drawLine(graphics, origin, new Point(sensor.getXDistance(), sensor.getYDistance()), true, new Color(255,i*(255/(enemySensors.length - 1)),0));
        }

        // Draw powerup sensors
        for(int i=0; i<powerupSensors.length; i++)
        {
            Sensor sensor = powerupSensors[i];
            if(sensor.getType() != 0)
                LevelScene.drawLine(graphics, origin, new Point(sensor.getXDistance(), sensor.getYDistance()), true, new Color(0,255,i*(255/(powerupSensors.length-1))));
        }

        // Draw pit sensors
        if(pitWidth > 0)
            LevelScene.drawRect(graphics, origin.x + pitDistance, 235, pitWidth * 16, 10, Color.PINK);

        LevelScene.drawStringDropShadow(graphics,"Wall Distance: "+ wallDistance, 0,  9, 1 );
        LevelScene.drawStringDropShadow(graphics,"Wall Height: "  + wallHeight  , 0, 10, 1 );

        byte[] data = new byte[TOTAL_SENSOR_BYTES];

        int offset = 0;

        // Luigi specific data
        data[offset++] = (byte)(observation.getMarioMode());
        data[offset++] = (byte)(observation.mayMarioJump()?1:0);
        data[offset++] = (byte)(observation.canShoot()?1:0);

        // Wall sensor data
        data[offset++] = (byte)(wallDistance / 2);
        data[offset++] = (byte)(wallHeight);

        // Pit sensor data
        data[offset++] = (byte)(pitDistance / 2);
        data[offset++] = (byte)(pitWidth);

        // NOTE: byte values range from -128 to +127
        //   the max distance possible in one direction in pixels is 11*16 = 176, which is out of range for a byte
        //   Thus, dividing the distance by 2 gives the new range of -88 to +88, which is within the byte range
        //   This value gets normalized before being used as input anyway, so this won't matter in the long run

        // Enemy sensor data
        for(int i=0; i<enemySensors.length; i++)
        {
            data[offset++] = (byte)(enemySensors[i].getXDistance() / 2);
            data[offset++] = (byte)(enemySensors[i].getYDistance() / 2);
        }

        // Powerup sensor data
        for(int i=0; i<powerupSensors.length; i++)
        {
            data[offset++] = (byte)(powerupSensors[i].getXDistance() / 2);
            data[offset++] = (byte)(powerupSensors[i].getYDistance() / 2);
        }

        return data;
    }                   

    private static int getBlockValue(Sensor block)
    {
        switch(block.getType())
        {
            case Sprite.KIND_FIRE_FLOWER:
            case Sprite.KIND_MUSHROOM:
                return 2;
            case 21:
                return 1;
            case 16:
            default:
                return 0;
        }
    }

    public boolean[] getAction(Environment observation, Graphics graphics)
    {
        sensorData = generateSensors(observation, graphics);        

        return getAction(observation);
    }

    public boolean[] getAction(Environment observation)
    {
        String str = new String(sensorData);
        server.sendSafe("Env:"+str);
        
        String input = server.recvSafe();
        String[] parts = input.split(":");
        if(parts.length == 2)
        {
            String tag = parts[0];
            String data = parts[1];
            if(tag.equals("Input"))
            {
                processInput(data);
            }
        }
        else
        {
            System.out.println("Error in received input: " + input);
        }

        return action;
    }
	
    private void processInput(String input)
    {
        if(input.length() == 5)
        {
            action[Mario.KEY_LEFT] = (input.charAt(0) == '1');
            action[Mario.KEY_RIGHT] = (input.charAt(1) == '1');
            action[Mario.KEY_DOWN] = (input.charAt(2) == '1');
            action[Mario.KEY_JUMP] = (input.charAt(3) == '1');
            action[Mario.KEY_SPEED] = (input.charAt(4) == '1');
        }
        else
		{
            System.out.println("Received too many controller commands: " + input);
		}
	}
			
    public AGENT_TYPE getType()
    {
        return AGENT_TYPE.AI;
    }

    public String getName() {        return name;    }

    public void setName(String Name) { this.name = Name;    }

        public void keyPressed (KeyEvent e)
    {
        toggleKey(e.getKeyCode(), true);
        System.out.println("sdf");
    }

    public void keyReleased (KeyEvent e)
    {
        toggleKey(e.getKeyCode(), false);
    }


    private void toggleKey(int keyCode, boolean isPressed)
    {
        switch (keyCode) {
            case KeyEvent.VK_LEFT:
                action[Mario.KEY_LEFT] = isPressed;
                break;
            case KeyEvent.VK_RIGHT:
                action[Mario.KEY_RIGHT] = isPressed;
                break;
            case KeyEvent.VK_DOWN:
                action[Mario.KEY_DOWN] = isPressed;
                break;

            case KeyEvent.VK_S:
                action[Mario.KEY_JUMP] = isPressed;
                break;
            case KeyEvent.VK_A:
                action[Mario.KEY_SPEED] = isPressed;
                break;
        }
    }
}