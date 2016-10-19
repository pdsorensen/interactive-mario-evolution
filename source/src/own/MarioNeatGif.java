package own;

import iec.GenotypeGif;
import iec.GifSequenceWriter;
import iec.MarioGIF;

import java.io.File;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.List;

import javax.imageio.stream.FileImageOutputStream;
import javax.imageio.stream.ImageOutputStream;

import org.jgap.Chromosome;
import org.jgap.Genotype;

import com.anji.util.Properties;

import ch.idsia.benchmark.mario.engine.GlobalOptions;
import ch.idsia.tools.MarioAIOptions;

public class MarioNeatGif extends MarioNeat {
	
	static MarioFitnessFunction ff;
	public int folderName = 0; 
	
	public void run() throws Exception {
		
		Date runStartDate = Calendar.getInstance().getTime();
		logger.info( "Run: start" );
		DateFormat fmt = new SimpleDateFormat( "HH:mm:ss" );
		
		boolean wait = false;
		
		for ( int generation = 0; generation < numEvolutions; generation++ ) {
			System.out.println("*************** Running generation: " + generation + " ***************"); 
			Date generationStartDate = Calendar.getInstance().getTime();
			logger.info( "Generation " + generation + ": start" );
			//genotype.evolve();
			
			//Chromosome c = genotype.getFittestChromosome();
			//bestChroms.add(c);
			
			//Reset MarioGIF object
			MarioGIF.reset(folderName);
			
			//GET CHROMOSOMES
			List<Chromosome> chroms = genotype.getChromosomes();
			
			//Folder structure
			new File("db/gifs/" + folderName).mkdirs();
			
			for (int i = 0; i < chroms.size(); i++) {
			    Chromosome chrommie = (Chromosome) chroms.get(i);
			    
			    //Record images from playtrough
			    ff.recordImages(chrommie, generation);
			    
			    // Create GIFS from those images
			    GifSequenceWriter.createGIF("db/gifs/" + folderName + "/");   
			}
			
			
			GifSequenceWriter.fileNumber = 0; 
			//Set wait to true
			wait = true;

			MarioGIF.runIEC(folderName, populationSize);

			
			while(wait){
				Thread.sleep(10);
				
				
				//Check if chromosome has been chosen
				if(MarioGIF.getChosenGif() != -1){
					MarioGIF.setVisibility(false);
					System.out.println( "THE CHOSEN ONE IS #" + MarioGIF.getChosenGif() );
					
					//Set all chroms fitness to zero
					for (Chromosome c : chroms)
						c.setFitnessValue(0);
					
					//Get chosen chromosome
				 	Chromosome theChosenChrom = (Chromosome) chroms.get( MarioGIF.getChosenGif() );
				 	
				 	//Set its fitness
				 	System.out.println("set fitness go!");
				 	theChosenChrom.setFitnessValue(100);

					genotype.evolveGif();
					
					//MarioGIF.deleteGifs("./db/gifs/" + folderName);

					//Stop waiting and continue evolution with next generation
					wait = false;

				}
			}
				
			// generation finish
			Date generationEndDate = Calendar.getInstance().getTime();
			long durationMillis = generationEndDate.getTime() - generationStartDate.getTime();
			logger.info( "Generation " + generation + ": end [" + fmt.format( generationStartDate )
					+ " - " + fmt.format( generationEndDate ) + "] [" + durationMillis + "]" );
			folderName++;
		}
		
	}
	
	
	
	public static void main( String[] args ) throws Throwable {

		Properties props = new Properties( "mario.properties" );
		ff = new MarioFitnessFunction(); 
		ff.init(props);
		
		try {
			System.out.println("Booting up!");
		    
		    //NEAT SETUP
			MarioNeat mNeat = new MarioNeatGif();
			mNeat.init(props);
			mNeat.trainWithIEC();
		
			System.out.println("Last up!");
			
		}
		catch ( Throwable th ) {
			System.out.println(th);
		}
	
		
		//MarioFitnessFunction ff = new MarioFitnessFunction(); 
		//ff.init(props);
		/*for(int i = 0; i<bestChroms.size(); i++){
			System.out.println("GENERATION " + i + " - BestFitness(" + bestChroms.get(i).getFitnessValue() + ")"); 
			ff.evaluate(bestChroms.get(i), true);
		}*/
		
		
	}
	
}
