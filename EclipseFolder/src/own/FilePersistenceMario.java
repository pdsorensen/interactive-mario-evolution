package own;

import java.io.FileOutputStream;

import org.jgap.Chromosome;

import com.anji.integration.XmlPersistableChromosome;
import com.anji.persistence.FilePersistence;
import com.anji.util.XmlPersistable;



public class FilePersistenceMario extends FilePersistence {
	public int counter = 0; 
	
	public void storeToFolder(Chromosome c, String path) throws Exception{
		System.out.println("STORING TO FOLDER");
		storeXmlToFolder( new XmlPersistableChromosome( c ), path );
	}

	public void storeXmlToFolder(XmlPersistable xp, String path) throws Exception{
		System.out.println("STORING TO XML FOLDER");
		FileOutputStream out = null;

		try {
			System.out.println("Saving to gifs folder");
			out = new FileOutputStream( path + Integer.toString(counter) );
			out.write( xp.toXml().getBytes() );
			out.close();
		}
		finally {
			if ( out != null )
				out.close();
		}
	}
}
