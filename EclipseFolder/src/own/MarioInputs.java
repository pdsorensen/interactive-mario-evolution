package own;

import ch.idsia.benchmark.mario.environments.Environment;
import ch.idsia.benchmark.mario.environments.MarioEnvironment;

public class MarioInputs {
    
	static Environment environment = MarioEnvironment.getInstance();
    protected byte[][] levelScene;
    int zLevelScene = 0;
    int zLevelEnemies = 0;
    
    boolean includeStage, includeNearestEnemies, includeMarioState;
    
	int radNorth, radEast, radSouth, radWest;
	int radCenter = 9;
	
	int numNearestEnemies;
	
	/**
	 * constructor
	 */
	public MarioInputs( boolean includeStage, int north, int east, int south, int west, 
						boolean includeNearestEnemies, int numNearestEnemies,
						boolean includeMarioState ){
		
		this.includeStage = includeStage;
		radNorth = north;
		radEast = east;
		radSouth = south;
		radWest = west;
		
		this.includeNearestEnemies = includeNearestEnemies;
		this.numNearestEnemies = numNearestEnemies;
		
		this.includeMarioState = includeMarioState;
		
		//Print total number of inputs
		System.out.println( "Total inputs for Neat: " + getNumInputs() );
	}
	
	/**
	 * 
	 * @return the total number of inputs used for NEAT
	 */
	private int getNumInputs(){
		
		int numInputs = 0;
		
		if(includeStage)
			numInputs += getNumStageInputs();
		
		if(includeNearestEnemies)
			numInputs += numNearestEnemies * 2;
		
		if(includeMarioState)
			numInputs++;
		
		return numInputs;
	}
	
	/**
	 * @return the number of tiles in the grid used for presenting the
	 * the level for Mario
	 */
	private int getNumStageInputs(){
		
		int x = radEast + radWest + 1;
		int y = radNorth + radSouth + 1;
		
		return x * y;
	}
	

	/**
	 * Gets all the different inputs depended on what has been
	 * included in the constructor of the MarioInputs object
	 * @return
	 */
	public double[] getAllInputs(){
		
		double[] networkInput = new double[0];
		
		
		if(includeStage){
			//Get state of the world
			double[] limitedStateInput = getLimitedStateFromStage();
			networkInput = addArrays(networkInput, limitedStateInput);
		}
		
		if(includeNearestEnemies){
			//Get three nearest enemies
			double[] inputNearestEnemies = getClosestEnemiesInput();
			networkInput = addArrays(networkInput, inputNearestEnemies);
		}
		
		if(includeMarioState){
			//Get the state of Mario
			double[] marioStateInput = getMarioStateInput();
			networkInput = addArrays(networkInput, marioStateInput);
		}
		
		return networkInput;
	}
	
	/**
	 * Concatenates the two parameter arrays
	 * @param arr1 the first array to be concat'ed
	 * @param arr2 the second array to be concat'ed
	 * @return the concatenated arrays
	 */
	public double[] addArrays( double[] arr1, double[] arr2 ){
		
		double[] both = new double[ arr1.length + arr2.length ];
		
		//Add first array
		System.arraycopy(arr1, 0, both, 0, arr1.length);
		
		//Add second array
		System.arraycopy(arr2, 0, both, arr1.length, arr2.length);
		
		return both;
	}
	
	
	/**
	 * @return the state of mario:
	 * -1 = small
	 *  0 = big
	 *  1 = can shoot fire
	 */
	public double[] getMarioStateInput(){
		
		double[] marioState = new double[1];
		marioState[0] = environment.getEvaluationInfo().marioMode;
		
		//Normalize the input between -1 and 1
		marioState[0] -= 1;
		
		return marioState;
		
	}
	
	
	///////GET CLOSEST ENEMIES
	/**
	 * @return The distance and angle to up to 3 nearest enemies.
	 */
	public double[] getClosestEnemiesInput(){
		
		//Get enemy positions relative to Mario
		float[] enemies = environment.getEnemiesFloatPos();
		
		//Create input array
		double[] inputs = new double[ numNearestEnemies * 2 ];
		
		//Initial maxDistance, to be updated
		double maxDistance = -1;
		
		//Add angles and distance to enemies
		for(int i = 0; i < enemies.length && i < numNearestEnemies * 3; i += 3){
			
			//Get relative X and Y distance to Mario
			double relX = enemies[i+1];
			double relY = enemies[i+2];
			
			//Calculate distance to Mario
			double distance = Math.sqrt( Math.pow(relX, 2) + Math.pow(relY, 2) );
			
			//If this enemy is the farthest, update maxDistance
			if( distance > maxDistance )
				maxDistance = distance;
			
			//get angle in radians to mario
			double rad = Math.atan2(relX, relY);
			
			//Convert angle to degrees
			double degree = rad * ( 180 / Math.PI);
			
			//Get index in input array
			int k = i * 2 / 3;

			//Add distance to array
			inputs[ k ] = distance;
			
			//Add normalized angle to array
			inputs[ k + 1 ] = degree;
		}
		
		
		//Normalize distances and angles to enemies
		for(int i = 0; i < inputs.length; i += 2){
			inputs[i] = normalizeEnemyDistance( inputs[ i ], maxDistance );
			inputs[ i + 1 ] = normalizeDegree( inputs[ i + 1 ] );
		}
		
		return inputs;
	}
	
	
	/**
	 * @param degree to be normalized
	 * @return the normalized degree with value between -1 and 1
	 */
	private double normalizeDegree(double degree){
		
		double minDegree = -180;
		double maxDegree = 180;
		
		double normDegree = ( degree - minDegree ) / ( maxDegree - minDegree ) * 2 - 1;

		return normDegree;	
	}
	
	private double normalizeEnemyDistance( double distance, double maxDistance ){
		
		double minDistance = 0;
		
		double normEnemyDist = ( distance - minDistance ) / ( maxDistance - minDistance ) * 2 - 1;
		
		return normEnemyDist;
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
	 * @return the gridsystem from levelScene rotated in counter-clockwise direction
	 */
	private double[][] getRotatedLevelScene(){
		
		levelScene = environment.getMergedObservationZZ(zLevelScene, zLevelEnemies);
		
		int lengthX = levelScene.length;
		int lengthY = levelScene[0].length;
		
		double[][] rotatedLevelScene = new double[ lengthX ][ lengthY ];
		
		//"Rotate" the levelScene in counter-clockwise direction
		for(int i = 0; i < lengthX; i++ )
			for(int j = 0; j < lengthY; j++ )
				rotatedLevelScene[ lengthY - 1 - i ][ j ] = levelScene[ j ][ lengthY - 1 - i ];
		
		return rotatedLevelScene;
	}

	
	/*
	 * @param state, the double array with limited state of stage
	 * @return the values from state put into a single dimension array
	 */
	private double[] getTwoDimToOneDimArray(double[][] state){
		
		//Get the length of X and Y dimension of state array
		int lengthX = state.length;
		int lengthY = state[0].length;
		
		//Create single dimension array with same amount of space
		double[] newArray = new double[lengthX * lengthY];
		
		//Index number for the newArray
		int index = 0;
		
		//Go through double dimension array and put values in single dimension array
		for(int i = 0; i < lengthX; i++){
			for(int j = 0; j < lengthY; j++){
				newArray[ index ] = state[i][j];
				index++;
			}
		}
		
		//Normalize values in newArray
		double[] normNewArray = normalizeState(newArray);
		
		return normNewArray;
	}
	
	
	/*
	 * Array containing the values for the limited state of the stage
	 * 
	 */
	private double[] normalizeState(double[] input){
		
		double[] normInput = new double[input.length];
		
		double maxInput = -10000;
		double minInput = 10000;
		
		//Find highest and lowest value in input
		for(int i = 0; i < input.length; i++)
			if( input[i] > maxInput )
				maxInput = input[i];
			else
				if( input[i] < minInput )
					minInput = input[i];
		
		//This will take an approximate possible max and min input for whole system
		//Comment to make min and max based on all possible values and not 
		//the current values in input variable
//		maxInput = 100;
//		minInput = -127;
		
		double span = maxInput - minInput;
		
		//Normalize all values between -1 and 1
		for(int i = 0; i < input.length; i++)
			normInput[i] = ( ( input[i] - minInput ) / span * 2 ) - 1;
		
		//Return normalized input
		return normInput;
	}
	

	public double[] getLimitedStateFromStage(){
		
		//Get dimension lengths
		int xDimension = getXdimensionLength();
		int yDimension = getYdimensionLength();

		//Create array
		double[][] limitedState = new double[ xDimension ][ yDimension ];
		
		//Get level with normal X and Y axis
		double[][] rotatedLevel = getRotatedLevelScene();
		
		
		//Put desired tiles into the limitedState array
		for( int i = 0; i < xDimension; i++ )
			for( int j = 0; j < yDimension; j++ )
				limitedState[ i ][ j ] = rotatedLevel[ i + getStartX() ][ j + getStartY() ];
		
		
		//Convert to single dimension array
		double[] input = getTwoDimToOneDimArray(limitedState);
		
		return input;
	}
	
	
	/**
	 * Set radius for all 4 directions;
	 * This will specify the area that is used for the limited stage
	 */
	public void setRadius(int north, int east, int south, int west){	
		radNorth = north;
		radEast = east;
		radSouth = south;
		radWest = west;
		
		//Print num inputs
		int x = radEast + radWest + 1;
		int y = radNorth + radSouth + 1;
		int numInputs = x * y;
		//System.out.println("Total stage grid is " + numInputs + " inputs!");
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
		int startX = radCenter - radWest;
		return startX;
	}
	
	private int getEndX(){
		int endX = getStartX() +  getXdimensionLength();
		return endX;
	}
	
	private int getStartY(){
		int startY = radCenter - radNorth;
		return startY;
	}
	
	private int getEndY(){
		int endY = getStartY() +  getYdimensionLength();
		return endY;
	}
	
	public double[] getHardcodedInputs(){
		double jumping = (environment.isMarioAbleToJump()) ? 1 : 0;
		double shooting = (environment.isMarioAbleToShoot()) ? 1 : 0;
		double onGround = (environment.isMarioOnGround()) ? 1 : 0;
		double[] inputs = {jumping, shooting, onGround};
		return inputs; 
	}
	
}
