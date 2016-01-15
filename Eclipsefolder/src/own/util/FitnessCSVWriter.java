package own.util;

import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;

import com.opencsv.CSVReader;
import com.opencsv.CSVWriter;

public class FitnessCSVWriter {
	private final static String DELIMITER = ",";
	private final static String NEW_LINE_SEPERATOR = "\n";
	private final static String FILE_HEADER = "id, fitness";
	
	private static void generateCsvFile(String fileName, int[] values)
	   {
		try
		{
		    FileWriter writer = new FileWriter("db/csv/" + fileName);
		    
		    writer.append(FILE_HEADER); 
		    writer.append(NEW_LINE_SEPERATOR);
		    
			for(int i = 0; i<values.length; i++){
				writer.append(Integer.toString(i));
				writer.append(DELIMITER);
				writer.append(Integer.toString(values[i]));
				writer.append(NEW_LINE_SEPERATOR);
			} 

		    writer.flush();
		    writer.close();
		}
		catch(IOException e)
		{
		     e.printStackTrace();
		} 
	    }
	
	
	public static void main( String[] args ) throws IOException {
		int[] i = {120, 240, 40, 60, 1200};
		generateCsvFile("fitnessValues.csv", i);
		
		
	}
}
