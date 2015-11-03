package own;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Iterator;
import java.util.List;

import org.apache.log4j.Logger;
import org.jgap.BulkFitnessFunction;
import org.jgap.Chromosome;

import com.anji.integration.Activator;
import com.anji.integration.TargetFitnessFunction;
import com.anji.integration.TranscriberException;
import com.anji.util.Configurable;
import com.anji.util.Properties;

public class MarioFitnessFunction implements BulkFitnessFunction, Configurable {

	private static Logger logger = Logger.getLogger( TargetFitnessFunction.class );
	
	@Override
	public void init(Properties props) throws Exception {
		// TODO Auto-generated method stub
		
	}

	@Override
	public void evaluate(List genotypes) {
		// TODO Auto-generated method stub
		
		Iterator it = genotypes.iterator();
		while ( it.hasNext() ) {
			Chromosome genotype = (Chromosome) it.next();
			
			//GET MARIO TO PLAY...
		}
	}

	@Override
	public int getMaxFitnessValue() {
		// TODO Auto-generated method stub
		return 0;
	}
	
	
	
}
