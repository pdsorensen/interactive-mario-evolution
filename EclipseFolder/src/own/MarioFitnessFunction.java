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
    double[] state; 
    double[][] fullState;
    double[][] limitedState;
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
		double[] networkInput;
		//levelScene = environment.getLevelSceneObservationZ(zLevelScene);
	    levelScene = environment.getMergedObservationZZ(zLevelScene, zLevelEnemies);
	    
	    //state = newState();
	    int reach = 2;
	    setRadius(reach, reach, reach, reach);
		
		while(!environment.isLevelFinished()){
			//Set all actions to false
			resetActions();
			
//			#For each tick do
				
				//GET STATE
//				state = getStateFromStage();
				fullState = getFullStateFromStage();
				limitedState = getLimitedStateFromStage();
				
				//networkInput = new double[ levelScene.length * levelScene[0].length ];
				networkInput = new double[ getXdimensionLength() * getYdimensionLength() ];
				
				//SET INPUTS
//				networkInput = state;
				//networkInput = getTwoDimToOneDimArray(fullState);
				networkInput = getTwoDimToOneDimArray(limitedState);
				//System.out.println("Xdim: " + getXdimensionLength() +  " Ydim: " + getYdimensionLength());
				//System.out.println("networkInput: " + networkInput.length);
				//Give the network some inputs
				double[] networkOutput = activator.next(networkInput);

				//Perform some action based on networkOutput
				environment.performAction(getAction(networkOutput));
				makeTick();
				
				float[] enemies = environment.getEnemiesFloatPos();
				//if(enemies.length > 0)
					//for(int i = 0; i < enemies.length; i++)
						//System.out.println("enemies[" + i + "]: " + enemies[i] + " out of ");
				//float[] mario = environment.getMarioFloatPos();
				//System.out.println("mario at: " + mario[0]);
				
					//System.out.println("eniemes: " + enemies.length);
				
	    }
		

		//NORMALIZED RESULTS
		fitness += getFitnessDistancePassed(1);
		fitness += getFitnessQuick(10);
		fitness += getFitnessGreedy(100);
		fitness += getFitnessAgressive(1);
		fitness += getFitnessVariedAgressive(1, 1, 1);
		
		//UNNORMALIZED RESULTS
		//fitness += getFitnessMushroomsAndFlowers(100,100);
		//fitness += getFitnessMode(1);
		
		//double networkOutput = activator.next( networkInput )[ 0 ];
		//logger.debug( "trial took " + currentTimestep + " steps" );
		
		//To account for the casting to int
		fitness *= 10000;
		
		return (int)fitness;
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
	

	
	/*
	 * UNORMALIZED RESULTS
	 */
	public int getFitnessMushroomsAndFlowers(int ratioMushrooms, int ratioFlowers){
		
		int fitness = 0;
		
		//Mushrooms
		fitness += environment.getEvaluationInfo().mushroomsDevoured * ratioMushrooms;
		//Flowers
		fitness += environment.getEvaluationInfo().flowersDevoured * ratioFlowers;
		
		return fitness;
	}
	
	public int getFitnessMode(int quotient){
		
		int mode = environment.getEvaluationInfo().marioMode;
		int passedCells = environment.getEvaluationInfo().distancePassedCells;
		
		int fitness = 0;
		if(passedCells > 255) //Only if goal reached
			if(mode == 0) //Small
				fitness += 0 * quotient;
			else
				if(mode == 1) //Large
					fitness += 1000 * quotient;
				else
					if(mode == 2) //Fire
						fitness += 10000 * quotient;
					else
						fitness += 0;
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
	public double[] getStateFromStage(){
		
		double[] inputs =  { levelScene[ 9 ][ 10 ], levelScene[9][11]};

		return inputs;	
	}
	
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
	
	private double[][] getLimitedStateFromStage(){
		
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
		
		return inputs;
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
		int yDimension = ( radCenter + radEast ) - ( radCenter - radWest ) + 1;
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
}
