package own;

import java.io.File;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.Date;
import java.util.List;

import org.apache.log4j.Logger;
import org.jgap.BulkFitnessFunction;
import org.jgap.Chromosome;
import org.jgap.Configuration;
import org.jgap.Genotype;
import org.jgap.event.GeneticEvent;

import com.anji.Copyright;
import com.anji.integration.LogEventListener;
import com.anji.integration.PersistenceEventListener;
import com.anji.integration.PresentationEventListener;
import com.anji.neat.Evolver;
import com.anji.neat.NeatConfiguration;
import com.anji.persistence.Persistence;
import com.anji.polebalance.DoublePoleBalanceFitnessFunction;
import com.anji.run.Run;
import com.anji.util.Configurable;
import com.anji.util.DummyConfiguration;
import com.anji.util.Properties;
import com.anji.util.Reset;

import ch.idsia.agents.Agent;
import ch.idsia.benchmark.mario.environments.Environment;
import ch.idsia.benchmark.mario.environments.MarioEnvironment;
import ch.idsia.benchmark.tasks.BasicTask;
import ch.idsia.tools.MarioAIOptions;
import iec.GenotypeGif;
import iec.GifSequenceWriter;
import iec.MarioGIF;

public class MarioNeat implements Configurable{

	protected static Logger logger = Logger.getLogger( Evolver.class );
	
	/**
	 * properties key, # generations in run
	 */
	public static final String NUM_GENERATIONS_KEY = "num.generations";

	/**
	 * properties key, fitness function class
	 */
	public static final String FITNESS_FUNCTION_CLASS_KEY = "fitness_function";

	private static final String FITNESS_THRESHOLD_KEY = "fitness.threshold";

	private static final String RESET_KEY = "run.reset";

	/**
	 * properties key, target fitness value - after reaching this run will halt
	 */
	public static final String FITNESS_TARGET_KEY = "fitness.target";

	private static NeatConfiguration config = null;

	private Chromosome champ = null;

	Genotype genotype = null;
	
	GenotypeGif genotypeGif = null;

	public int numEvolutions = 0;

	private double targetFitness = 0.0d;

	private double thresholdFitness = 0.0d;

	private int maxFitness = 0;

	private Persistence db = null;
	
	// FOR FINDING THE BEST CHROMOSONES FOR EACH RUN: 
	static ArrayList<Chromosome> bestChroms = new ArrayList<Chromosome>(); 
	
	// FOR GIF CREATION 
	public int folderName = 0; 
	public static MarioFitnessFunction ff = new MarioFitnessFunction();
	static ArrayList<Chromosome> iecCandidates = new ArrayList<Chromosome>(); 
	/**
	 * ctor; must call <code>init()</code> before using this object
	 */
	public MarioNeat() {
		super();
	}
	
	@Override
	public void init(Properties props) throws Exception {
		
		boolean doReset = props.getBooleanProperty( RESET_KEY, false );
		if ( doReset ) {
			logger.warn( "Resetting previous run !!!" );
			Reset resetter = new Reset( props );
			resetter.setUserInteraction( false );
			resetter.reset();
		}

		config = new NeatConfiguration( props );

		// peristence
		db = (Persistence) props.singletonObjectProperty( Persistence.PERSISTENCE_CLASS_KEY );

		numEvolutions = props.getIntProperty( NUM_GENERATIONS_KEY );
		targetFitness = props.getDoubleProperty( FITNESS_TARGET_KEY, 1.0d );
		thresholdFitness = props.getDoubleProperty( FITNESS_THRESHOLD_KEY, targetFitness );

		//
		// event listeners
		//

		// run
		// TODO - hibernate
		Run run = (Run) props.singletonObjectProperty( Run.class );
		db.startRun( run.getName() );
		config.getEventManager().addEventListener( GeneticEvent.GENOTYPE_EVALUATED_EVENT, run );

		// logging
		LogEventListener logListener = new LogEventListener( config );
		config.getEventManager().addEventListener( GeneticEvent.GENOTYPE_EVOLVED_EVENT, logListener );
		config.getEventManager()
				.addEventListener( GeneticEvent.GENOTYPE_EVALUATED_EVENT, logListener );

		// persistence
		PersistenceEventListener dbListener = new PersistenceEventListener( config, run );
		dbListener.init( props );
		config.getEventManager().addEventListener(
				GeneticEvent.GENOTYPE_START_GENETIC_OPERATORS_EVENT, dbListener );
		config.getEventManager().addEventListener(
				GeneticEvent.GENOTYPE_FINISH_GENETIC_OPERATORS_EVENT, dbListener );
		config.getEventManager().addEventListener( GeneticEvent.GENOTYPE_EVALUATED_EVENT, dbListener );

		// presentation
		PresentationEventListener presListener = new PresentationEventListener( run );
		presListener.init( props );
		config.getEventManager().addEventListener( GeneticEvent.GENOTYPE_EVALUATED_EVENT,
				presListener );
		config.getEventManager().addEventListener( GeneticEvent.RUN_COMPLETED_EVENT, presListener );

		// fitness function
		BulkFitnessFunction fitnessFunc = (BulkFitnessFunction) props
				.singletonObjectProperty( FITNESS_FUNCTION_CLASS_KEY );
		config.setBulkFitnessFunction( fitnessFunc );
		maxFitness = fitnessFunc.getMaxFitnessValue();

		// load population, either from previous run or random
		genotype = db.loadGenotype( config );
		genotypeGif = (GenotypeGif) db.loadGenotype( config );
		
		if ( genotype != null )
			logger.info( "genotype from previous run" );
		else {
			genotype = Genotype.randomInitialGenotype( config );
			logger.info( "random genotype" );
		}
		
		// Might get messy: 
		ff.init(props);

	}

	
	public void run() throws Exception {
		// TODO: Cleaning and evaluating, think about class variables, more proper logging
		logger.info( "Run: start" );
		DateFormat fmt = new SimpleDateFormat( "HH:mm:ss" );
		
		boolean wait = false;
		for(int IECGeneration = 0; IECGeneration < 50; IECGeneration++){
			// IEC STEP
			System.out.println("*************** Running IECgeneration: " + IECGeneration + " ***************"); 
			logger.info( "Generation " + IECGeneration + ": start" );
		
			//Reset MarioGIF object and create new .gif folder
			MarioGIF.reset(folderName);
			new File("db/gifs/" + folderName).mkdirs();

			// RECORDING STEP
			List<Chromosome> chroms = genotype.getChromosomes();
			
			for (int i = 0; i < 9; i++) {
				//Get a chromosome
			    Chromosome chrommie = (Chromosome) chroms.get(i);
			    //Record that chromosome
			    ff.recordImages( chrommie, IECGeneration );
			    //Create and save gif 
			    GifSequenceWriter.createGIF("db/gifs/" + folderName + "/");   
			}
			
			ff.generation++;
			System.out.println("Generation after record: " + ff.generation + " | " + ff);
			
			
			MarioGIF.runIEC(folderName);
			
			wait = true;
			while(wait){
				Thread.sleep(10);
				//Check if chromosome has been chosen
				if(MarioGIF.getChosenGif() != -1){
					
					MarioGIF.setVisibility(false);
					
					//Set all chroms fitness to zero
					for (Chromosome c : chroms)
						c.setFitnessValue(0);
					
					//Get chosen chromosome
				 	Chromosome theChosenChrom = (Chromosome) chroms.get( MarioGIF.getChosenGif() );
				 	bestChroms.add(theChosenChrom);
				 	//Set it's fitness
				 	theChosenChrom.setFitnessValue(100);

					genotype.evolveGif();
					
					MarioGIF.deleteGifs("./db/gifs/" + folderName);

					//Stop waiting and continue evolution
					wait = false;

				}
			}
			iecCandidates.clear();
			GifSequenceWriter.fileNumber = 0; 
			folderName++;
			
			// AUTOMATED NEATSTEP WITH DISTANCE PASSED AS FITNESS
			for ( int generation = 0; generation < 2; generation++ ) {
				System.out.println("Running generation: " + generation + "..."); 
				Date generationStartDate = Calendar.getInstance().getTime();
				logger.info( "Automated NEAT Generation " + generation + ": start" );
				
				genotype.evolve();
				
				//Chromosome c = genotype.getFittestChromosome();
				//iecCandidates.add(c);
				
				ff.generation++;
				System.out.println("Generation in NEAT loop: " + ff.generation + " | " + ff);
				
				// generation finish
				Date generationEndDate = Calendar.getInstance().getTime();
				long durationMillis = generationEndDate.getTime() - generationStartDate.getTime();
				logger.info( "Generation " + generation + ": end [" + fmt.format( generationStartDate )
						+ " - " + fmt.format( generationEndDate ) + "] [" + durationMillis + "]" );
			}
		}
	}
	
	public static void main( String[] args ) throws Throwable {
		Properties props = new Properties( "mario.properties" );
		try {
			System.out.println("Booting up!");
		    
		    //NEAT SETUP
			MarioNeat mNeat = new MarioNeat();
			mNeat.init(props);
			mNeat.run();
		
			System.out.println("Last up!");
			
		}
		catch ( Throwable th ) {
			System.out.println(th);
		}
		
		MarioFitnessFunction ff = new MarioFitnessFunction(); 
		ff.init(props);
		for(int i = 0; i<bestChroms.size(); i++){
			System.out.println("GENERATION " + i + " - BestFitness(" + bestChroms.get(i).getFitnessValue() + ")"); 
			ff.evaluate(bestChroms.get(i), true);
		}
		
		// Load in chromosome: 
//		String chromId = "244";
//		Persistence db = (Persistence) props.newObjectProperty( Persistence.PERSISTENCE_CLASS_KEY );
//		Chromosome chrom = db.loadChromosome( chromId, config );
//		if ( chrom == null )
//			throw new IllegalArgumentException( "no chromosome found.");
//		
//		ff.evaluate(chrom, true);
		
		
	}
	
}
