/*
 * Copyright (c) 2009-2010, Sergey Karakovskiy and Julian Togelius
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Mario AI nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

package ch.idsia.scenarios;

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

public class Custom  {
	static Environment environment = MarioEnvironment.getInstance();
	static Hashtable<Integer, Boolean> levelImagePoints = new Hashtable<Integer, Boolean>();
	
	int CELLS_IN_LEVEL = environment.getEvaluationInfo().levelLength; 
	
public static void main(String[] args)
{
	//29 cells for a picture. A cell is 16pixels. 16*29 = 464
	//49 cells for next picture
	//69 for next picture
	//80 for next picture etc
	// So incrementing until end
	String options = "-lt 0 -ls 0 -ld 0 -mix 9  -miy 140";
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

    //return true;
    for(int j = 0; j < 1; j++) {
    	basicTask.reset();
    	System.out.println("ALLRIGHT");
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
//	        	ff.getAllInputs();
//				ff.drawGrid();
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
	            //environment.getEvaluationInfo();
	            
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
				//System.out.println("I SHOULD TAKE A PICTURE: " + currentMarioCell);
				//environment.createLevelImage();
				levelImagePoints.replace(i, true);
			}
		}
	}
}