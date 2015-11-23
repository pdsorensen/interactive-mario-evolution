package own;

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

import com.anji.util.Properties;

public class MarioNeatGif extends MarioNeat {
	
	static MarioFitnessFunction ff;
	
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
			
			
			
			//GET CHROMOSOMES
			List chroms = genotype.getChromosomes();
			
			
			
			
			for (int i = 0; i < chroms.size(); i++) {
			    Chromosome chrommie = (Chromosome) chroms.get(i);
			    
			    //Record images from playtrough
			    ff.recordImages(chrommie);
			    
			    ImageOutputStream output = 
			            new FileImageOutputStream(new File("db/gifs/" + i + ".gif"));
			    GifSequenceWriter gsw = new GifSequenceWriter(output, 5, 1, false);
			    
			    //Create gif from recorded images
			    gsw.createGIF();
			    
			}
			
			// generation finish
			Date generationEndDate = Calendar.getInstance().getTime();
			long durationMillis = generationEndDate.getTime() - generationStartDate.getTime();
			logger.info( "Generation " + generation + ": end [" + fmt.format( generationStartDate )
					+ " - " + fmt.format( generationEndDate ) + "] [" + durationMillis + "]" );
			
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
			mNeat.run();
		
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
		
		MarioGIF.runIEC();
	}
	
}
