package iec;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Iterator;
import java.util.List;

import org.jgap.BulkFitnessFunction;
import org.jgap.Chromosome;
import org.jgap.Configuration;
import org.jgap.FitnessFunction;
import org.jgap.Genotype;
import org.jgap.InvalidConfigurationException;
import org.jgap.MutationOperator;
import org.jgap.NaturalSelector;
import org.jgap.ReproductionOperator;
import org.jgap.Specie;
import org.jgap.event.GeneticEvent;

public class GenotypeGif extends Genotype implements Serializable {

	
	public GenotypeGif(Configuration a_activeConfiguration, List a_initialChromosomes)
			throws InvalidConfigurationException {
		super(a_activeConfiguration, a_initialChromosomes);
	}


	/**
	 * 
	 */
	private static final long serialVersionUID = 1L;

	public synchronized void evolve() {
		System.out.println("Running genotypeGIF");

		try {
			System.out.println("Running genotypeGIF");
			m_activeConfiguration.lockSettings();

			// Fire an event to indicate we've evaluated all chromosomes.
			// -------------------------------------------------------
			m_activeConfiguration.getEventManager().fireGeneticEvent(
					new GeneticEvent( GeneticEvent.GENOTYPE_EVALUATED_EVENT, this ) );

			// Select chromosomes to survive.
			// ------------------------------------------------------------
			NaturalSelector selector = m_activeConfiguration.getNaturalSelector();
			selector.add( m_activeConfiguration, m_chromosomes );
			m_chromosomes = selector.select( m_activeConfiguration );
			selector.empty();

			// Repopulate the population of species and chromosomes with those selected
			// by the natural selector, and cull species down to contain only remaining
			// chromosomes.
			Iterator speciesIter = m_species.iterator();
			while ( speciesIter.hasNext() ) {
				Specie s = (Specie) speciesIter.next();
				s.cull( m_chromosomes );
				if ( s.isEmpty() )
					speciesIter.remove();
			}
			
			// Fire an event to indicate we're starting genetic operators. Among
			// other things this allows for RAM conservation.
			// -------------------------------------------------------
			m_activeConfiguration.getEventManager().fireGeneticEvent(
					new GeneticEvent( GeneticEvent.GENOTYPE_START_GENETIC_OPERATORS_EVENT, this ) );

			// Execute Reproduction Operators.
			// -------------------------------------
			Iterator iterator = m_activeConfiguration.getReproductionOperators().iterator();
			List offspring = new ArrayList();
			while ( iterator.hasNext() ) {
				ReproductionOperator operator = (ReproductionOperator) iterator.next();
				operator.reproduce( m_activeConfiguration, m_species, offspring );
			}

			// Execute Mutation Operators.
			// -------------------------------------
			Iterator mutOpIter = m_activeConfiguration.getMutationOperators().iterator();
			while ( mutOpIter.hasNext() ) {
				MutationOperator operator = (MutationOperator) mutOpIter.next();
				operator.mutate( m_activeConfiguration, offspring );
			}

			// in case we're off due to rounding errors
			Collections.shuffle( offspring, m_activeConfiguration.getRandomGenerator() );
			adjustChromosomeList( offspring, m_activeConfiguration.getPopulationSize()
					- m_chromosomes.size() );

			// add offspring
			// ------------------------------
			addChromosomesFromMaterial( offspring );

			// Fire an event to indicate we're starting genetic operators. Among
			// other things this allows for RAM conservation.
			// -------------------------------------------------------
			m_activeConfiguration.getEventManager().fireGeneticEvent(
					new GeneticEvent( GeneticEvent.GENOTYPE_FINISH_GENETIC_OPERATORS_EVENT, this ) );

			// Fire an event to indicate we've performed an evolution.
			// -------------------------------------------------------
			m_activeConfiguration.getEventManager().fireGeneticEvent(
					new GeneticEvent( GeneticEvent.GENOTYPE_EVOLVED_EVENT, this ) );
		}
		catch ( InvalidConfigurationException e ) {
			throw new RuntimeException( "bad config", e );
		}
	}
	
}
