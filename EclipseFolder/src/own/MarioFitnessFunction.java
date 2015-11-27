package own;

import java.nio.DoubleBuffer;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Iterator;
import java.util.List;

import org.apache.log4j.Logger;
import org.jgap.BulkFitnessFunction;
import org.jgap.Chromosome;
import org.jgap.Configuration;

import com.anji.integration.Activator;
import com.anji.integration.ActivatorTranscriber;
import com.anji.integration.TargetFitnessFunction;
import com.anji.integration.TranscriberException;
import com.anji.persistence.Persistence;
import com.anji.util.Arrays;
import com.anji.util.Configurable;
import com.anji.util.DummyConfiguration;
import com.anji.util.Properties;

import ch.idsia.agents.Agent;
import ch.idsia.benchmark.mario.engine.GlobalOptions;
import ch.idsia.benchmark.mario.engine.sprites.Mario;
import ch.idsia.benchmark.mario.environments.Environment;
import ch.idsia.benchmark.mario.environments.MarioEnvironment;
import ch.idsia.benchmark.tasks.BasicTask;
import ch.idsia.tools.MarioAIOptions;

public class MarioFitnessFunction implements BulkFitnessFunction, Configurable {

	private static Logger logger = Logger.getLogger( TargetFitnessFunction.class );
	private ActivatorTranscriber factory;
	private int numTrials = 3;
	private int numTrialsBeforeIEC = 0;;
	
	//MARIO VARIABLES
	static final MarioAIOptions marioAIOptions = new MarioAIOptions();
    static final BasicTask basicTask = new BasicTask(marioAIOptions);

    static Environment environment = MarioEnvironment.getInstance();
    static Agent agent = new NEATController();
    
    //Info on stage
    protected byte[][] mergedObservation;
    //Control buttons
    boolean[] actions = new boolean[Environment.numberOfKeys]; 

    public static int generation = 0;
    public static int prevGeneration = 0;
    public static int difficulty = 0;
    public static int level = 0;
    public static int seed = 2;
	
	//Define the inputs for Mario
	MarioInputs marioInputs = new MarioInputs(  true, 1, 1, 1, 1, 
												true, 3,
												true, true, true, true );
	
	
	@Override
	public void init(Properties props) throws Exception {
		System.out.println("INITTING");
		factory = (ActivatorTranscriber) props.singletonObjectProperty( ActivatorTranscriber.class );		
	}

	@Override
	public void evaluate(List genotypes) {
		System.out.println("Evaluting list of chromosones..."); 
		Iterator it = genotypes.iterator();
		while ( it.hasNext() ) {
			Chromosome genotype = (Chromosome) it.next();
			evaluate(genotype, true);
		}
	}

	
	public void increaseGeneration(){
		generation++;
	}
	
	public void changeSeed(int num){
		seed += num;
	}
	
	/*
	 * If enough playthroughs have been made:
	 * 		Increase difficulty
	 * 		Change state
	 * 		Increase seed
	 */
	public void setStage(){
		//Check if generation changes
		if(prevGeneration != generation){
			System.out.println("GENERATION HAS CHANGED");
			//Change difficulty
			if( generation % 4 == 0 ) 
				difficulty++;
			
			//Change seed
			if( generation % 8 == 0 ){
				difficulty = 0;
				seed++;
			}
			
			//Change level
			if( generation % 16 == 0 ){
				difficulty = 0;
				seed = 0;
				level++;
			}
			
			prevGeneration = generation;	
		}
		
		System.out.println("level: " + level + " | diff: " + difficulty + " | seed: " + seed);
	    //marioAIOptions.setVisualization(false);
		marioAIOptions.setLevelDifficulty( difficulty );
	    marioAIOptions.setLevelType( level );
	    marioAIOptions.setLevelRandSeed( seed );
		environment.reset(marioAIOptions);
		
	}
	
	
	/*
	 * @param index used for naming the gifs,
	 */
	public void recordImages( Chromosome c, int generation ){
		
		//Set stage, difficulty and seed
		setStage();
	    
		//Turn on recording
		marioAIOptions.setVisualization(true);
		environment.recordMario(true);
		
		int gifDurationMillis = 2000;
		int delayRecording = 1000;
		
		//Delay gif with generation
		int generationDelay = ( generation * 200 );
		delayRecording += generationDelay;
		gifDurationMillis += generationDelay;
		
		try {
			// Load in chromosome to the factory
			Activator activator = factory.newActivator( c );		
			
		    singleTrialForGIF( activator, gifDurationMillis, delayRecording );

		}
		catch ( Throwable e ) {
			logger.warn( "error evaluating chromosome " + c.toString(), e );
			c.setFitnessValue( 0 );
		}
		
	}
	
private void singleTrialForGIF( Activator activator, int gifDurationMillis, int delayRecording ) {

	    //Get millis at starting point
	    long startMillis = System.currentTimeMillis();
	    
	    //Run trial
		while(!environment.isLevelFinished() && startMillis + gifDurationMillis > System.currentTimeMillis()){			
			
			//Begin recording after some seconds
			if(environment.getEvaluationInfo().timeSpent >= ( delayRecording / 1000 ) )	
				environment.recordMario(true);
				
				//Set all actions to false
				resetActions();
			
				//Get inputs
				double[] networkInput = marioInputs.getAllInputs();
				drawInputs(networkInput);
				//Feed the inputs to the network
				double[] networkOutput = activator.next(networkInput);
				
				//Perform some action based on networkOutput
				environment.performAction(getAction(networkOutput));
				makeTick();		
		}
	}


	/**
	 * Evaluate for the automated Neat step
	 * @param c
	 * @param visual
	 */
	public void evaluate( Chromosome c, boolean visual ) {
		
		// Reset environment each trial
		if(visual){
			marioAIOptions.setVisualization(true);
		} else {
			marioAIOptions.setVisualization(false);
		}
	    
	    try {
			Activator activator = factory.newActivator( c );
			
			// calculate fitness, sum of multiple trials
			int fitness = 0;
			for ( int i = 0; i < numTrials; i++ ){

				setStage();
				
				fitness += singleTrial( activator );
				
				changeSeed( 1 );
			}
			//Reset seed
			changeSeed( -numTrials );
			
			fitness /= numTrials;
			System.out.println("EVALUATE: fitness score,  " + fitness);
			c.setFitnessValue( fitness );
		}
		catch ( Throwable e ) {
			logger.warn( "error evaluating chromosome " + c.toString(), e );
			c.setFitnessValue( 0 );
		}
	}
	

	
	private int singleTrial( Activator activator ) {
		double fitness = 0;

	    marioInputs.setRadius(1, 1, 1, 1);
	    
		while(!environment.isLevelFinished()){
			//Set all actions to false
			resetActions();
			
			//Get inputs
			double[] networkInput = marioInputs.getAllInputs();
			//System.out.println("Network size: " + networkInput.length);
			//Feed the inputs to the network
			double[] networkOutput = activator.next(networkInput);
			drawInputs(networkInput);
			
			//Perform some action based on networkOutput
			environment.performAction(getAction(networkOutput));
			 
			makeTick();		
	    }
		

		//NORMALIZED RESULTS
		fitness += getFitnessDistancePassed(1);
		//fitness += getFitnessQuick(1);
		//fitness += getFitnessGreedy(1);
		//fitness += getFitnessAgressive(1);
		//fitness += getFitnessVariedAgressive(1, 1, 1);
		//fitness += getFitnessMushroomsAndFlowers(1,1);
		//fitness += getFitnessExplore(1, 1);
		fitness *= getFitnessMode( 1.2, 1.5, 2.0 );
		
		//To account for the casting to int
		fitness *= 10000;
		
		return (int)fitness;
	}
	
	public void drawInputs(double[] inputsToBeDrawn){
		int marioX = (int) environment.getMarioFloatPos()[0];
		int marioY = (int) environment.getMarioFloatPos()[1] - 8;

		String[] inputValues = marioInputs.getHardcodedCellValues();
		System.out.println("NORTH:  " + inputValues[0]); 
		System.out.println("WEST:  " + inputValues[1]); 
		System.out.println("EAST:  " + inputValues[2]); 
		System.out.println("SOUTH:  " + inputValues[3]); 
		// NORTH
		environment.drawLine(marioX, marioY, marioX, marioY-16, inputValues[0]);
		// WEST
		environment.drawLine(marioX, marioY, marioX-16, marioY, inputValues[1]);
		// EAST
		environment.drawLine(marioX, marioY, marioX+16, marioY, inputValues[2]);
		//SOUTH
		environment.drawLine(marioX, marioY, marioX, marioY+16, inputValues[3]);
	}
	
	/*
	 * NORMALIZED FITNESS ELEMENTS
	 */

	/*
	 * @return normalized distance that Mario have travelled in the stage
	 */
	public double getFitnessDistancePassed(double ratio){
		
		double levelLength = environment.getEvaluationInfo().levelLength;
		double distancePassed = environment.getEvaluationInfo().distancePassedCells;

		double fitness = distancePassed / levelLength * ratio;
		return fitness;
	}
	
	/*
	 * @return normalized time left of stage
	 */
	
	public double getFitnessQuick(int ratio){
		
		int passedCells = environment.getEvaluationInfo().distancePassedCells;
		double fitness = 0;
		
		double timeLeft = environment.getEvaluationInfo().timeLeft;
		double totalTime = timeLeft + environment.getEvaluationInfo().timeSpent;
		
		
		if(passedCells > 255){ //Only if goal reached
			
			fitness = timeLeft / totalTime * ratio;
			System.out.println("timefitness: " + fitness);
		}
		
		return fitness;
	}
	
	/*
	 * @return Normalized value of amount of coins collected
	 */
	
	public double getFitnessGreedy(double ratio){
		
		double totalNumberOfCoins = environment.getEvaluationInfo().totalNumberOfCoins;
		double coinsCollected = environment.getEvaluationInfo().coinsGained;
		
		double fitness = coinsCollected / totalNumberOfCoins * ratio;
		return fitness;
	}
	
	/*
	 * @return Normalized result of creatures killed
	 */
	
	public double getFitnessAgressive(double ratio){
		
		double totalCreatures = environment.getEvaluationInfo().totalNumberOfCreatures;
		double totalKills = environment.getEvaluationInfo().killsTotal;
		
		double fitness = totalKills / totalCreatures * ratio;
		return fitness;
	}
	
	/*
	 * @return Normalized result of creatures killed, varied by kill method
	 */
	public double getFitnessVariedAgressive(double ratioStomp, double ratioFire, double ratioShell){
		
		double fitness = 0;
		
		double totalCreatures = environment.getEvaluationInfo().totalNumberOfCreatures;
		
		double shellKills = environment.getEvaluationInfo().killsByShell * ratioShell;
		double fireKills = environment.getEvaluationInfo().killsByFire * ratioFire;
		double stompKills = environment.getEvaluationInfo().killsByStomp * ratioStomp;
		
		fitness += shellKills / totalCreatures * ratioShell;
		fitness += fireKills / totalCreatures * ratioFire;
		//System.out.println("FireKills: " + fitness );
		fitness += stompKills / totalCreatures * ratioStomp;
		//System.out.println("+ stompKills: " + fitness );
		
		return fitness;
	}
	
	public double getFitnessMushroomsAndFlowers(int ratioMushrooms, int ratioFlowers){
		
		double fitness = 0;
		
		double totalFlowers = environment.getEvaluationInfo().totalNumberOfFlowers;
		double totalMushrooms = environment.getEvaluationInfo().totalNumberOfMushrooms;
		
		double flowersEaten = environment.getEvaluationInfo().flowersDevoured;
		double mushroomsEaten = environment.getEvaluationInfo().mushroomsDevoured;
		
		//Mushrooms
		if(totalFlowers > 0)
			fitness += flowersEaten / totalFlowers * ratioMushrooms;
		//Flowers
		if(totalMushrooms > 0)
			fitness += mushroomsEaten / totalMushrooms * ratioFlowers;
		
		System.out.println("-----");
		System.out.println("flowersEaten: " + flowersEaten + " / " + totalFlowers);
		System.out.println("mushroomsEaten: " + mushroomsEaten + " / " + totalMushrooms);
		
		
		return fitness;
	}
	
	public double getFitnessExplore(int ratioHiddenBlocks, int ratioPowerUps){
		
		double fitness = 0;
		
//		double totalFlowers = environment.getEvaluationInfo().totalNumberOfFlowers;
//		double totalMushrooms = environment.getEvaluationInfo().totalNumberOfMushrooms;
//
//		fitness += ( totalFlowers + totalMushrooms ) * ratioPowerUps;
		
		double totalHiddenBlocks = environment.getEvaluationInfo().totalNumberOfHiddenBlocks;
		double hiddenBlocksFound = environment.getEvaluationInfo().hiddenBlocksFound;
		
		fitness += hiddenBlocksFound / totalHiddenBlocks * ratioHiddenBlocks;
		
		return fitness;
	}
	
	
	/*
	 * UNORMALIZED RESULTS
	 */
	public double getFitnessMode(double rewardSmall, double rewardBig, double rewardFire){
		
		double fitness = 0;
		
		int mode = environment.getEvaluationInfo().marioMode;
		int passedCells = environment.getEvaluationInfo().distancePassedCells;
		
		//If goal reached
		if(passedCells > 255) 
			if(mode == 0) //Small
				fitness = rewardSmall;
			else
				if(mode == 1) //Large
					fitness = rewardBig;
				else
					if(mode == 2) //Fire
						fitness = rewardFire;
					else
						fitness += 1;
		//if goal not reached
		else 
			fitness = 1;
		
		return fitness;
	}
	
	
	public boolean[] getAction(double[] networkOutput){
		
		if(networkOutput[0] < 0.5)
			actions[Mario.KEY_LEFT] = false;
		else
			actions[Mario.KEY_LEFT] = true;
		
		if(networkOutput[1] < 0.5)
			actions[Mario.KEY_RIGHT] = false;
		else
			actions[Mario.KEY_RIGHT] = true;
		
		if(networkOutput[2] < 0.5)
			actions[Mario.KEY_DOWN] = false;
		else
			actions[Mario.KEY_DOWN] = true;
		
		if(networkOutput[3] < 0.5)
			actions[Mario.KEY_UP] = false;
		else
			actions[Mario.KEY_UP] = true;
		
		if(networkOutput[4] < 0.5)
			actions[Mario.KEY_JUMP] = false;
		else
			actions[Mario.KEY_JUMP] = true;
		
		if(networkOutput[5] < 0.5)
			actions[Mario.KEY_SPEED] = false;
		else
			actions[Mario.KEY_SPEED] = true;
		
		return actions;
	}
	
	public void resetActions(){
	    actions[Mario.KEY_LEFT] = false;
	    actions[Mario.KEY_RIGHT] = false;
	    actions[Mario.KEY_DOWN] = false;
	    actions[Mario.KEY_UP] = false;
	    actions[Mario.KEY_JUMP] = false;
	    actions[Mario.KEY_SPEED] = false;
	}
	
	
	public void performAction(){
		
		environment.performAction(agent.getAction());	
		
	}
	
	public void makeTick(){
		environment.tick();
    	agent.integrateObservation(environment);
        
	}
	
	@Override
	public int getMaxFitnessValue() {
		return 10;
	}
	
	/*
	 * MARIO ENVIRONMENT FUNCTIONS
	 */
	
	/**
	 * 
	 * @param level type
	 * @param level difficulity 
	 * @param seed
	 */
	public void setMarioLevel(int level, int difficulity, int seed){
		marioAIOptions.setLevelType(level);
		marioAIOptions.setLevelDifficulty(difficulity);
	    marioAIOptions.setLevelRandSeed(seed);
	    environment.reset(marioAIOptions);
	}
}
