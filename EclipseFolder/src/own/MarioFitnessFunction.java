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
	
	
	//MARIO VARIABLES
	static final MarioAIOptions marioAIOptions = new MarioAIOptions();
    static final BasicTask basicTask = new BasicTask(marioAIOptions);

    static Environment environment = MarioEnvironment.getInstance();
    static Agent agent = new NEATController();
    
    //Info on stage
    int detectionRadius = 1;
    int radNorth, radEast, radSouth, radWest;
    int radCenter = 9;
    int zLevelScene = 0;
    int zLevelEnemies = 0;
    protected byte[][] levelScene;
    protected byte[][] mergedObservation;
    //Control buttons
    boolean[] actions = new boolean[Environment.numberOfKeys]; 
    
	
	
	@Override
	public void init(Properties props) throws Exception {
		// TODO Auto-generated method stub

		System.out.println("INITTING");
		factory = (ActivatorTranscriber) props.singletonObjectProperty( ActivatorTranscriber.class );		
		//marioAIOptions.printOptions(false);
	}

	@Override
	public void evaluate(List genotypes) {
		System.out.println("Evaluting list of chromosones..."); 
		Iterator it = genotypes.iterator();
		while ( it.hasNext() ) {
			Chromosome genotype = (Chromosome) it.next();
			evaluate(genotype, false);
		}
		
		//marioAIOptions
	}
	
	
	/*
	 * @param index used for naming the gifs,
	 */
	public void recordImages( Chromosome c ){

	    //marioAIOptions.setVisualization(false);
		environment.reset(marioAIOptions);
	    
		//Turn on recording
		environment.recordMario(false);
		
		int gifDurationMillis = 5000;
		
		try {
			Activator activator = factory.newActivator( c );
			
			marioAIOptions.setLevelDifficulty(0);
		    marioAIOptions.setLevelType(0);
		    marioAIOptions.setLevelRandSeed(0);

		    singleTrialForGIF( activator, gifDurationMillis );
		}
		catch ( Throwable e ) {
			logger.warn( "error evaluating chromosome " + c.toString(), e );
			c.setFitnessValue( 0 );
		}
		
	}
	
private void singleTrialForGIF( Activator activator, int gifDurationMillis ) {
		
	    //levelScene = environment.getMergedObservationZZ(zLevelScene, zLevelEnemies);

	    //Set radius of input grid
	    setRadius(1, 3, 0, 3);
	    
	    //Get millis at starting point
	    long startMillis = System.currentTimeMillis();
	    
	    //Run trial
		while(!environment.isLevelFinished() && startMillis + gifDurationMillis > System.currentTimeMillis()){			
			
			//Begin recording after some seconds
			if(environment.getEvaluationInfo().timeSpent >= 0)	
				environment.recordMario(true);
				
			
			//Set all actions to false
			resetActions();
			
			//GET INPUTS
				//create input array
				double[] networkInput = new double[0];
				
				//Get state of the world
				double[] limitedStateInput = getLimitedStateFromStage();
				networkInput = addArrays(networkInput, limitedStateInput);
				
				//Get three nearest enemies 
				double[] inputNearestEnemies = getClosestEnemiesInput();
				networkInput = addArrays(networkInput, inputNearestEnemies);
				
				//Get the state of Mario
				double[] marioStateInput = getMarioStateInput();
				networkInput = addArrays(networkInput, marioStateInput);
				
				double[] hardcodedInputs = getHardcodedInputs();
				networkInput = addArrays(networkInput, hardcodedInputs);
				
				System.out.println("Size: " + networkInput.length);
				
				//Feed the inputs to the network
				double[] networkOutput = activator.next(networkInput);
				
				//Perform some action based on networkOutput
				environment.performAction(getAction(networkOutput));
				makeTick();		
		}
	}


	public void evaluate( Chromosome c, boolean visual ) {
		// Easy level: 
		String options = "-lf off -zs 1 -ls 16 -vis on";
	    environment.reset(options);
		
		// Reset environment each trial
		if(visual){
			marioAIOptions.setVisualization(true);
			environment.reset(marioAIOptions);
		} else {
			marioAIOptions.setVisualization(false);
			environment.reset(marioAIOptions);
		}
	    
	    try {
			Activator activator = factory.newActivator( c );

			//marioAIOptions.setVisualization(false);
			
			// calculate fitness, sum of multiple trials
			int fitness = 0;
			for ( int i = 0; i < numTrials; i++ ){
				if(i == 0){
					marioAIOptions.setLevelDifficulty(0);
				    marioAIOptions.setLevelType(0);
				    marioAIOptions.setLevelRandSeed(0);
				} 
				
				if(i == 1){
					marioAIOptions.setLevelDifficulty(0);
				    marioAIOptions.setLevelType(1);
				    marioAIOptions.setLevelRandSeed(20);
				} 
				
				if(i == 2) {
					marioAIOptions.setLevelDifficulty(1);
				    marioAIOptions.setLevelType(1);
				    marioAIOptions.setLevelRandSeed(5);
				}
				environment.reset(marioAIOptions);
				fitness += singleTrial( activator );
			}
			
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
		//logger.debug( "state = " + Arrays.toString( state ) );
		
		//levelScene = environment.getLevelSceneObservationZ(zLevelScene);
	    levelScene = environment.getMergedObservationZZ(zLevelScene, zLevelEnemies);
	    
	    int reach = 2;
	    setRadius(2, 4, 0, 4);
	    
		while(!environment.isLevelFinished()){
			//Set all actions to false
			resetActions();
			
//			#For each tick do
			
			//GET INPUTS
				//create input array
				double[] networkInput = new double[0];
				
				//Get state of the world
				double[] limitedStateInput = getLimitedStateFromStage();
				networkInput = addArrays(networkInput, limitedStateInput);
				//System.out.println("Grid Size: " + limitedStateInput.length);
				//Get direction and distance to nearest enemies
				double[] inputNearestEnemies = getClosestEnemiesInput();
				networkInput = addArrays(networkInput, inputNearestEnemies);
				
				//Get the state of Mario
				double[] marioStateInput = getMarioStateInput();
				networkInput = addArrays(networkInput, marioStateInput);
				
				//Feed the inputs to the network
				double[] networkOutput = activator.next(networkInput);
				
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
	
	/*
	 * @param arr1 is the first array to be concatenated
	 * @oaram arr2 the second array to be concatenated
	 * @return arr1 and arr2 in a single array
	 */
	public double[] addArrays( double[] arr1, double[] arr2 ){
		
		double[] both = new double[ arr1.length + arr2.length ];
		
		//Add first array
		System.arraycopy(arr1, 0, both, 0, arr1.length);
		
		//Add second array
		System.arraycopy(arr2, 0, both, arr1.length, arr2.length);
		
		return both;
	}
	
	
	
	/*
	 * @return The distance and angle to up to 3 nearest enemies.
	 */
	public double[] getClosestEnemiesInput(){
		
		float[] enemies = environment.getEnemiesFloatPos();
		
		int maxEnemies = 3;
		double[] inputs = new double[ maxEnemies * 2 ];
		
		//Reset array
		for(int i = 0; i < maxEnemies * 2; i++)
			inputs[i] = 0;
		
		//Add angles and distance to enemies
		for(int i = 0; i < enemies.length && i < 9; i += 3){
			
			double relX = enemies[i+1];
			double relY = enemies[i+2];
			
			double distance = Math.sqrt( Math.pow(relX, 2) + Math.pow(relY, 2) );
			
			//get angle in radians to mario
			double rad = Math.atan2(relX, relY);
			
			//Convert angle to degrees
			double degrees = rad * ( 180 / Math.PI);
			
			//Get index in input array
			int k = i * 2 / 3;
			//System.out.println("k: " + k + " out of " + enemies.length);
			//Add distance to array
			inputs[ k ] = distance;
			
			//Add angle to array
			inputs[ k + 1 ] = degrees;
		}
		
		return inputs;
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
	
	
	
	
	
	private double[] newState() {
		double[] state = new double[ 2 ];
		state[ 0 ] = state[ 1 ] =  0;
		return state;
	}
	
	/*
	 * @return The state of the Mario World. Atm only the two by three blocks in front of Mario
	 */
//	public double[] getStateFromStage(){
//		
//		double[] inputs =  { levelScene[ 9 ][ 10 ], levelScene[9][11]};
//
//		return inputs;	
//	}
	
	
	private double[] getMarioStateInput(){
		
		double[] marioState = new double[1];
		marioState[0] = environment.getEvaluationInfo().marioMode;
		
		return marioState;
		
	}
	
	
	/*
	 * @return an empty double array in the size of the full stage.
	 */
	private double[][] newFullState(){
		
		double[][] state = new double[ levelScene.length ][ levelScene[0].length ];
		
		for(int i = 0; i < levelScene.length; i++)
			for(int j = 0; j < levelScene[i].length; j++)
				state[ i ][ j ] = 0;
		
		return state;
	}
	
	/*
	 * @return The FULL state of the Mario World
	 */
	private double[][] getFullStateFromStage(){

		double[][] inputs = newFullState();
		
		for(int i = 0; i < levelScene.length; i++)
			for(int j = 0; j< levelScene[i].length; j++)
				inputs[ i ][ j ] = levelScene[ i ][ j ];
			
		return inputs;
	}
	
	private double[] getTwoDimToOneDimArray(double[][] state){
		
		double[] newArray = new double[state.length * state[0].length];
		
		for(int i = 0; i < state.length; i++){
			double[] row = state[i];
			
			for(int j = 0; i < row.length; i++){
				newArray[i * row.length + j] = state[i][j];
			}
		}
		
		return newArray;
	}
	
	private double[][] getBlankLimitedState(){
		
		//Calculate dimension lengths
		int xDimension = getXdimensionLength();
		int yDimension = getYdimensionLength();
		
		//Create array
		double[][] state = new double[ xDimension ][ yDimension ];
		
		//Reset array - is it necessary?
		for(int i = 0; i < xDimension; i++)
			for(int j = 0; j < yDimension; j++)
				state[ i ][ j ] = 0;
		
		
		return state;
	}
	
	private double[] getLimitedStateFromStage(){
		
		double[][] inputs = getBlankLimitedState();
		
		
		//Calculate dimension lengths
		int xDimension = getXdimensionLength();
		int yDimension = getYdimensionLength();
		
		//Create array
		double[][] state = new double[ xDimension ][ yDimension ];
		
		//Put values in the array
		for(int i = getStartX(); i < xDimension; i++)
			for(int j = getStartY(); j< yDimension; j++)
				inputs[ i ][ j ] = levelScene[ i ][ j ];
		
		//Convert to single dimension array
		double[] input = getTwoDimToOneDimArray(inputs);
		
		return input;
	}
	
	/*
	 * Set radius for all 4 directions;
	 */
	private void setRadius(int north, int east, int south, int west){	
		radNorth = north;
		radEast = east;
		radSouth = south;
		radWest = west;	
	}
	
	private int getXdimensionLength(){
		int xDimension = ( radCenter + radEast ) - ( radCenter - radWest ) + 1;
		return xDimension;
	}
	
	private int getYdimensionLength(){
		int yDimension = ( radCenter + radNorth ) - ( radCenter - radSouth ) + 1;
		return yDimension;
	}
	
	private int getStartX(){
		return radCenter - radWest;
	}
	private int getStartY(){
		return radCenter - radSouth;
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
		// TODO Auto-generated method stub
		return 10;
	}
	
	public double[] getHardcodedInputs(){
		double jumping = (environment.isMarioAbleToJump()) ? 1 : 0;
		double shooting = (environment.isMarioAbleToShoot()) ? 1 : 0;
		double onGround = (environment.isMarioOnGround()) ? 1 : 0;
		double[] inputs = {jumping, shooting, onGround};
		return inputs; 
	}
}
