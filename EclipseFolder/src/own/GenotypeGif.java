package own;

import java.io.Serializable;
import java.util.List;

import org.jgap.BulkFitnessFunction;
import org.jgap.Configuration;
import org.jgap.Genotype;
import org.jgap.InvalidConfigurationException;

public class GenotypeGif extends Genotype implements Serializable {

	
	public GenotypeGif(Configuration a_activeConfiguration, List a_initialChromosomes)
			throws InvalidConfigurationException {
		super(a_activeConfiguration, a_initialChromosomes);
	}


	/**
	 * 
	 */
	private static final long serialVersionUID = 1L;

//	public synchronized void evolve() {
//
//		try {
//			m_activeConfiguration.lockSettings();
//			
//			// If a bulk fitness function has been provided, then convert the
//			// working pool to an array and pass it to the bulk fitness
//			// function so that it can evaluate and assign fitness values to
//			// each of the Chromosomes.
//			// --------------------------------------------------------------
//			BulkFitnessFunction bulkFunction = m_activeConfiguration.getBulkFitnessFunction();
//			if ( bulkFunction != null ){
//				bulkFunction.evaluate( m_chromosomes );
//			}
//		}
//	}
	
}
