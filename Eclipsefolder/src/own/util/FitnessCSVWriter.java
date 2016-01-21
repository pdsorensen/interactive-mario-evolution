package own.util;

import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;

public class FitnessCSVWriter {
	private final static String DELIMITER = ";";
	private final static String NEW_LINE_SEPERATOR = "\n";
	private final static String FILE_HEADER = "id, fitness";
	
	public static void generateCsvFile(String fileName, int[] values, String name)
	{
		try
		{
		    FileWriter writer = new FileWriter("db/csv/" + name + "_" + fileName + ".csv");
		    
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
}
