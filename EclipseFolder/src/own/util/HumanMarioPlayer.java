package own.util;

import ch.idsia.agents.Agent;
import ch.idsia.agents.controllers.ForwardAgent;
import ch.idsia.agents.controllers.ForwardJumpingAgent;
import ch.idsia.agents.learning.SimpleMLPAgent;
import ch.idsia.benchmark.mario.engine.GlobalOptions;
import ch.idsia.benchmark.mario.environments.Environment;
import ch.idsia.benchmark.mario.environments.MarioEnvironment;
import ch.idsia.benchmark.tasks.BasicTask;
import ch.idsia.tools.MarioAIOptions;

import java.io.IOException;
import java.util.Hashtable;

import own.FSMController;
import own.MarioFitnessFunction;
import own.MarioInputs;
import own.NEATController;
import competition.gic2010.turing.sergeykarakovskiy.SergeyKarakovskiy_ForwardAgent;

/**
 * Created by IntelliJ IDEA.
 * User: Sergey Karakovskiy, sergey@idsia.ch
 * Date: May 7, 2009
 * Time: 4:38:23 PM
 * Package: ch.idsia
 */

public class HumanMarioPlayer  {
	
	static Environment environment = MarioEnvironment.getInstance();
	static Hashtable<Integer, Boolean> levelImagePoints = new Hashtable<Integer, Boolean>();
	
	int CELLS_IN_LEVEL = environment.getEvaluationInfo().levelLength; 
	
public static void main(String[] args)
{
	// 256 cells / rate of changing mario spawn position (5) = 51.2    
	
	// 9 cells is mario spawn
	//20 cells ahead for a picture (so next picture is 29). A cell is 16pixels. 16*29 = 464
	//49 for next picture
	//79 for next picture etc
	// incrementing until end
	String options = "-lt 0 -ls 0 -ld 0 -mix 300  -miy 170";
//	String options = "";
    final MarioAIOptions marioAIOptions = new MarioAIOptions(options);
    final BasicTask basicTask = new BasicTask(marioAIOptions);
    
    Environment environment = MarioEnvironment.getInstance();
    //Agent agent = new ForwardJumpingAgent(); 
    //Agent agent = new SimpleMLPAgent();
    //Agent agent = new NEATController();
    
    // FOR DEBUGGING DRAWING FUNCTIONS
    Agent agent = marioAIOptions.getAgent();
    
    marioAIOptions.setLevelDifficulty(0);
    marioAIOptions.setLevelType(0);
    marioAIOptions.setLevelRandSeed(0);
    environment.reset(options);
    MarioInputs marioInputs = new MarioInputs(  true, 1, 1, 1, 1, 
			true, 3,
			true, true, true, true );
    MarioFitnessFunction ff = new MarioFitnessFunction(); 
    
    //marioAIOptions.setLevelDifficulty(0);
    //marioAIOptions.setLevelRandSeed(0);
    //basicTask.setOptionsAndReset(marioAIOptions);
    int i = environment.getEvaluationInfo().levelLength; 
    System.out.println(i);
    //options = "-lf on -zs 1 -ls 16 -vis on -mix " + Integer.toString(i);
    //environment.reset(options);
    
    populateHashMap();
    environment.changeFPS();
    //return true;
    for(int j = 0; j < 1; j++) {
    	basicTask.reset();
	    while (!environment.isLevelFinished()){
		    environment.tick();
	        if (!GlobalOptions.isGameplayStopped){
	        	// Do Drawing! 
	        	//ff.resetActions();
				
				//Get inputs
	//			double[] networkInput = marioInputs.getAllInputs();
				
				//Feed the inputs to the network and translate it
	//			double[] networkOutput = activator.next(networkInput);
	//			boolean[] actions = getAction(networkOutput);
				
				//Drawing and debugging functions 
	        	ff.getAllInputs();
				ff.drawGrid();
//				ff.drawPossibleMarioActions();
//				ff.drawNearestEnemies(2);
	//			System.out.println(environment.getMarioEgoPos()[0]);
	//			ff.drawOutputs(actions);
	//			marioInputs.printAllOutputs(actions, networkOutput); 
	//			marioInputs.printAllInputs(networkInput);
	        	//System.out.println("Level Length: " + environment.getEvaluationInfo().levelLength);
	
	        	// Perform action 
				agent.integrateObservation(environment);
	            agent.giveIntermediateReward(environment.getIntermediateReward());
	            boolean[] action = agent.getAction();
	            environment.performAction(action);
	            //checkImagePoints(environment.getEvaluationInfo().distancePassedCells);
	             //environment.getEvaluationInfo().distancePassedCells;
	            //System.out.println( "distanceCellsPassed: " + environment.getEvaluationInfo().distancePassedCells );
	        }
	        //System.out.println(basicTask.getEnvironment().getEvaluationInfoAsString());
	    }
    }

    System.out.println(environment.getEvaluationInfo());
    System.exit(0);
}

	public static void populateHashMap(){
		// Cell 29 is the first place to take picture
		int imagePoint = 9;  
		levelImagePoints.put(imagePoint, false);
		
		for(int i = 0; i<11; i++){
			imagePoint += 20; 
			levelImagePoints.put(imagePoint, false);
		}
		
		for(int p : levelImagePoints.keySet()){
			System.out.println("Key: " + p);
		}
	}
	
	public static void checkImagePoints(int currentMarioCell){
		//System.out.println("Recieved value: " + currentMarioCell);
		for(int i : levelImagePoints.keySet()){
			if(currentMarioCell == i && levelImagePoints.get(i)== false){
				System.out.println("I SHOULD TAKE A PICTURE: " + currentMarioCell);
				//environment.createLevelImage();
				levelImagePoints.replace(i, true);
			}
		}
	}
}