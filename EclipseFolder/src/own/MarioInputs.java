package own;

import ch.idsia.benchmark.mario.environments.Environment;
import ch.idsia.benchmark.mario.environments.MarioEnvironment;

public class MarioInputs {
    
	static Environment environment = MarioEnvironment.getInstance();
    protected byte[][] levelScene;
    int zLevelScene = 0;
    int zLevelEnemies = 0;
    
    boolean includeStage, includeNearestEnemies, includeMarioState, includeJump, includeShoot, includeOnGround;
    
	int radNorth, radEast, radSouth, radWest;
	int radCenter = 9;
	
	public int numNearestEnemies;
	
	// FOR DEBUGGING MODE 
	StringBuilder sb = new StringBuilder(); 
	double[][] drawLimitedState;
	
	
	/**
	 * constructor. 
	 * Specify which inputs you would like to include for Mario. 
	 * There will be a print to the console specifying how many inputs are
	 * included in total.
	 */
	public MarioInputs( boolean includeStage, int north, int east, int south, int west, 
						boolean includeNearestEnemies, int numNearestEnemies,
						boolean includeMarioState,
						boolean includeJump,
						boolean includeShoot,
						boolean includeOnGround){
		
		this.includeStage = includeStage;
		radNorth = north;
		radEast = east;
		radSouth = south;
		radWest = west;
		
		this.includeNearestEnemies = includeNearestEnemies;
		this.numNearestEnemies = numNearestEnemies;
		
		this.includeMarioState = includeMarioState;
		
		this.includeJump = includeJump;
		this.includeShoot = includeShoot;
		this.includeOnGround = includeOnGround;
		
		//Print total number of inputs
		System.out.println( "Total inputs for Neat: " + getNumInputs() );
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
		
		if(includeJump){
			double[] marioAbleToJump = getAbleToJump();
			networkInput = addArrays(networkInput, marioAbleToJump);
		}
		
		if(includeShoot){
			double[] marioAbleToShoot= getAbleToShoot();
			networkInput = addArrays(networkInput, marioAbleToShoot);
		}
		
		if(includeOnGround){
			double[] marioOnGround = getOnGround();
			networkInput = addArrays(networkInput, marioOnGround);
		}
		
		return networkInput;
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
		
		if(includeJump)
			numInputs++;
		
		if(includeShoot)
			numInputs++;
		
		if(includeOnGround)
			numInputs++;
		
		return numInputs;
	}
	
	/**
	 * @return the number of tiles in the grid used for presenting the
	 * the level for Mario
	 */
	public int getNumStageInputs(){
		
		int x = radEast + radWest + 1;
		int y = radNorth + radSouth + 1;
		
		return x * y;
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
		
		levelScene = environment.getLevelSceneObservationZ(zLevelScene);
		//Uncomment to include enemies
		//levelScene = environment.getMergedObservationZZ(zLevelScene, zLevelEnemies);
		
		
		int lengthX = levelScene.length;
		int lengthY = levelScene[0].length;
		
		double[][] rotatedLevelScene = new double[ lengthX ][ lengthY ];
		
		//"Rotate" the levelScene in counter-clockwise direction
		for(int i = 0; i < lengthX; i++ )
			for(int j = 0; j < lengthY; j++ ){
				//rotatedLevelScene[ lengthY - 1 - i ][ j ] = levelScene[ j ][ lengthY - 1 - i ];
				rotatedLevelScene[ i ][ j ] = levelScene[ i ][ j ];
				//System.out.println("rotatedLevelScene: " + rotatedLevelScene[ i ][ j ]);
			}
				
		return rotatedLevelScene;
	}

	
	/*
	 * @param state, the double array with limited state of stage
	 * @return the values from state put into a single dimension array
	 */
	public double[] getTwoDimToOneDimArray(double[][] state){
		
		//Get the length of X and Y dimension of state array
		int lengthX = state.length;
		int lengthY = state[0].length;
		
		//Create single dimension array with same amount of space
		double[] inputs = new double[lengthX * lengthY];
		
		//Index number for the newArray
		int index = 0;
		
		//Go through double dimension array and put values in single dimension array
		for(int i = 0; i < lengthX; i++)
			for(int j = 0; j < lengthY; j++){
				inputs[ index ] = state[i][j];
				index++;
			}
		
		return inputs;
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
		maxInput = 2;
		minInput = -128;
		
		double span = maxInput - minInput;
		
		//Normalize all values between -1 and 1
		for(int i = 0; i < input.length; i++)
			normInput[i] = ( ( input[i] - minInput ) / span * 2 ) - 1;
		
		//Return normalized input
		return normInput;
	}
	
	public double[][] preprocessStateValues(double[][] limitedState){
		//double[][] preprocessedValues = new double[getXdimensionLength()][getYdimensionLength()];
		//System.out.println("");
		for(int i = 0; i<limitedState.length; i++){
			//System.out.println("");
			for(int j = 0; j<limitedState[i].length; j++){
				//System.out.println("LimitedState Value: " + limitedState[i][j]);
				if(limitedState[i][j] == -60 || limitedState[i][j] == -115 || limitedState[i][j] == -119){
					limitedState[i][j] = -60;
				}
			}
		}
		return limitedState; 
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
		
		// Set draw values
		setDrawValues(limitedState);
		
		limitedState = preprocessStateValues(limitedState);
		
		//Convert to single dimension array
		double[] inputs = getTwoDimToOneDimArray(limitedState);
		
		//Normalize values in newArray
		double[] normalizedInputs = normalizeState( inputs );
		
		return normalizedInputs;
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
	
	public int getXdimensionLength(){
		int xDimension = ( radCenter + radEast ) - ( radCenter - radWest ) + 1;
		return xDimension;
	}
	
	public int getYdimensionLength(){
		int yDimension = ( radCenter + radNorth ) - ( radCenter - radSouth ) + 1;
		return yDimension;
	}
	
	private int getStartX(){
		int startX = radCenter - radWest;
		return startX;
	}
	
	private int getStartY(){
		int startY = radCenter - radNorth;
		return startY;
	}

	
	private double[] getAbleToJump(){
		
		double[] jumping = new double[1];
		jumping[0] = ( environment.isMarioAbleToJump() ) ? 1 : -1;
		
		return jumping;
	}
	
	private double[] getAbleToShoot(){
		
		double[] canShoot = new double[1];
		canShoot[0] = ( environment.isMarioAbleToShoot() ) ? 1 : -1;
		
		return canShoot;
	}
	
	private double[] getOnGround(){
		
		double[] onGround = new double[1];
		onGround[0] = ( environment.isMarioOnGround() ) ? 1 : -1;
		
		return onGround;
	}
	
	public double[] getHardcodedInputs(){
		double jumping = (environment.isMarioAbleToJump()) ? 1 : 0;
		double shooting = (environment.isMarioAbleToShoot()) ? 1 : 0;
		double onGround = (environment.isMarioOnGround()) ? 1 : 0;
		double[] inputs = {jumping, shooting, onGround};
		return inputs; 
	}
	
	/********** DEBUGGING FUNCTIONS GOES HERE **********/ 
	public void setDrawValues(double[][] inputs){
		//System.out.println("Setting drawing values with inputs[" + inputs.length + "][" + inputs[0].length + "]");
		drawLimitedState = new double[inputs.length][inputs[0].length]; 
		for(int i = 0; i<inputs.length; i++){
			for(int j = 0; j<inputs[i].length; j++){
				drawLimitedState[i][j] = inputs[i][j];
			}
		}
		
		//printArray(inputs);
	} 
	
	public String[] getHardcodedCellValues(){
		String[] res = new String[drawLimitedState.length * drawLimitedState[0].length];
		res[0] = Double.toString(Math.round(drawLimitedState[0][1])); // NORTH
		res[1] = Double.toString(Math.round(drawLimitedState[0][0])); // NORTH WEST
		res[2] = Double.toString(Math.round(drawLimitedState[0][2])); // NORTH EAST 
		res[3] = Double.toString(Math.round(drawLimitedState[1][0])); // WEST 
		res[4] = Double.toString(Math.round(drawLimitedState[1][2])); // EAST
		res[5] = Double.toString(Math.round(drawLimitedState[2][1])); // SOUTH
		res[6] = Double.toString(Math.round(drawLimitedState[2][0])); // SOUTHWEST
		res[7] = Double.toString(Math.round(drawLimitedState[2][2])); // SOUTHEAST
		
		return res; 
	}
	
	public void printArray(double[][] arrayToPrint){
		sb.setLength(0);
		//System.out.println("Printing DoubleArray[" + arrayToPrint.length + "][" + arrayToPrint[0].length + "]");
		for(int i = 0; i<arrayToPrint.length; i++){
			for(int j = 0; j<arrayToPrint[i].length; j++){
				sb.append(arrayToPrint[i][j] + " - ");
			}
			sb.append("\n");
		}
		//System.out.println(sb.toString());
	}
	
	public void printScene(){
		sb.setLength(0);
		System.out.println("Printing levelScene[" + levelScene.length + "][" + levelScene[0].length + "]");
		for(int i = 0; i<levelScene.length; i++){
			for(int j = 0; j<levelScene[i].length; j++){
				if(i == 9 && j == 9){
					sb.append("M"); 
				} else {
					sb.append(levelScene[i][j]);
				}
				if(levelScene[i][j] == 0){
					sb.append("  ");
				}
			}
			sb.append("\n");
		}
		System.out.println(sb.toString());
	}
	
	public void printAllInputs(double[] inputs){
		int gridSize = getNumStageInputs(); boolean showHardcodedValues = false; 
		int counter = 0; 
		if(this.includeJump && this.includeOnGround && this.includeShoot)
			showHardcodedValues = true; 
		
		
		System.out.println("-----------  PRINTING OUT NEURAL NET[" + inputs.length + "] --------");
		System.out.println("***** GENERAL INFO *****");
		System.out.println("Gridsize: " + gridSize); 
		System.out.println("NumEnemies: " + numNearestEnemies); 
		System.out.println("Show marioState: " + this.includeMarioState);
		System.out.println("Show hardcoded values: " + showHardcodedValues);
		System.out.println("");
		
		System.out.println("****** NEURAL NET INPUTS ******* "); 
	    if(this.includeStage){
			for(int i = 0; i<gridSize; i++)
				System.out.println("*  Stage: " + inputs[i]);
			counter += gridSize; 
	    }
	    
	    if(this.includeNearestEnemies){
			for(int i = gridSize; i<counter+(numNearestEnemies*2); i++)
				System.out.println("*  Enemy: " + inputs[i]);
			counter += numNearestEnemies*2;
	    }
	    
	    if(this.includeMarioState){
	    	System.out.println("*  State: " + inputs[counter]);
	    	counter += 1; 
	    	if(showHardcodedValues){
	    		System.out.println("*  isMarioAbleToJump()  : " + inputs[counter]); // isMarioAbleToJump()
	    		System.out.println("*  isMarioAbleToShoot() : " + inputs[counter + 1]); // isMarioAbleToShoot()
	    		System.out.println("*  isMarioOnGround()    : " + inputs[counter + 2]); // isMarioOnGround()
	    		counter += 3; 
	    	}	
	    } else if(showHardcodedValues){
	    	System.out.println("*  isMarioAbleToJump()  : " + inputs[counter]); // isMarioAbleToJump()
    		System.out.println("*  isMarioAbleToShoot() : " + inputs[counter + 1]); // isMarioAbleToShoot()
    		System.out.println("*  isMarioOnGround()    : " + inputs[counter + 2]); // isMarioOnGround()
    		counter += 3; 
	    }
	    System.out.println("****** END OF NET INPUTS *******");
	    System.out.println(""); 
	}
	
	public void printAllOutputs(boolean[] actions, double[] outputs){
		System.out.println("");
		System.out.println("-------- PRINTING NET OUTPUTS[" + actions.length + "] -------");
		if(actions[0] == true)
			System.out.println("*  LEFT : " + actions[0] + "  - " + outputs[0]); 
		else 
			System.out.println("*  LEFT : "  + actions[0] + " - "  + outputs[0]); 

		if(actions[1] == true)
			System.out.println("*  RIGHT: " + actions[1] + "  - " + outputs[1]); 
		else 
			System.out.println("*  RIGHT: " + actions[1] + " - " + outputs[1]); 
		
		if(actions[2] == true)
			System.out.println("*  DOWN : " + actions[2] + "  - " + outputs[2]); 
		else 
			System.out.println("*  DOWN : " + actions[2] + " - " + outputs[2]); 
		
		if(actions[3] == true)
			System.out.println("*  UP   : " + actions[3] + "  - " + outputs[3]); 
		else 
			System.out.println("*  UP   : " + actions[3] + " - " + outputs[3]); 
		
		if(actions[5] == true)
			System.out.println("*  FIRE : " + actions[4] + "  - " + outputs[4]); 
		else 
			System.out.println("*  FIRE : " + actions[4] + " - " + outputs[4]); 
		
		if(actions[5] == true)
			System.out.println("*  JUMP : " + actions[5] + "  - " + outputs[5]); 
		else 
			System.out.println("*  JUMP : " + actions[5] + " - " + outputs[5]); 

		System.out.println("-------- END OF NET OUTPUTS  -------");
		System.out.println("");
	}
	

}
