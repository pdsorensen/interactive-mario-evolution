package own;

import java.io.FileOutputStream;

import org.jgap.Chromosome;

import com.anji.integration.XmlPersistableChromosome;
import com.anji.persistence.FilePersistence;
import com.anji.util.XmlPersistable;



public class FilePersistenceMario extends FilePersistence {
	public int counter = 0; 
	
	public void storeToFolder(Chromosome c, String path) throws Exception{
		storeXmlToFolder( new XmlPersistableChromosome( c ), path );
	}

	public void storeXmlToFolder(XmlPersistable xp, String path) throws Exception{
		System.out.println("Storing to xml folder");
		FileOutputStream out = null;

		try {
			out = new FileOutputStream( path + Integer.toString(counter) + ".xml" );
			out.write( xp.toXml().getBytes() );
			out.close();
			counter++;
		}
		finally {
			if ( out != null )
				out.close();
		}
	}
}
