package iec;

import java.awt.BorderLayout;
import java.awt.Dimension;
import java.awt.FlowLayout;
import java.awt.GridLayout;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.image.BufferedImage;
import java.io.File;
import java.util.ArrayList;

import javax.swing.BorderFactory;
import javax.swing.ImageIcon;
import javax.swing.JButton;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.SwingUtilities;
import javax.swing.border.EmptyBorder;

import own.MarioNeatGif;


public class MarioGIF {
	
	static int chosenGif;
	public static int iecGeneration = 1;
	//Frame parameters
	static JFrame frame;
	
	public static void reset(int folderName){
		
		//Create new frame
		frame = new JFrame(); 
		
		//Delete images
		//deleteGifs("./db/gifs/" + folderName + "/");
		
		//reset chosen chromosome number
		MarioGIF.setChosenGif(-1);
		
		//Show window
		MarioGIF.setVisibility(true);
	}
	
	public static void runIEC(int folder){
		
		Runnable r = new Runnable() {
			@Override
			public void run() {
				
				chosenGif = -1;
				
				//JLabel headerLabel = new JLabel("headerLabel",JLabel.CENTER );
				JPanel contentPane = new JPanel();
				
				
	            frame.setSize(new Dimension(1600, 1000));
	            frame.setTitle("Mario AI Evaluator" + Integer.toString(iecGeneration));
	            iecGeneration++; 
	            frame.setLayout(new GridLayout(0,1));
	           
	            
				// Get the GIFS into ImageIcons
	            contentPane.setLayout(new GridLayout(0,3));
				ImageIcon[] gifs = new ImageIcon[9];
				
			
				for(int i = 0; i<9;i++){
					String fileLocation = "./db/gifs/" + Integer.toString(folder) + "/" + new Integer(i).toString() + ".gif";
					System.out.println("Loading file at location: " + fileLocation);
					gifs[i] = new ImageIcon(fileLocation);
				}
				
				//frame.add(headerLabel);
				//frame.add(controlPanel);
				
				// Assign imageIcons and text to JComponents
				for(int i = 0; i<9;i++){
					String imageLabel = Integer.toString(i+1);
					JButton button = new JButton(imageLabel, gifs[i]);
					
					//Add actionListener
					ButtonListener buttonEar = new ButtonListener(i);
					button.addActionListener(buttonEar);
					
					//Set text
					button.setHorizontalTextPosition(JButton.CENTER);
					button.setVerticalTextPosition(JButton.BOTTOM);
					
					contentPane.add(button);
				}
				
				frame.add(contentPane);
	      	  	frame.setLocationRelativeTo(null);
	            frame.setVisible(true);
			}
		};
		
		SwingUtilities.invokeLater(r);
	}
	
	public static void deleteGifs(String folder){
		
		//Create path to directory
		final File dir = new File(folder);
	    
		//Create arraylist
		ArrayList<BufferedImage> images = new ArrayList<BufferedImage>(); 
	    
		//Get files from directory
		File[] imgFiles = dir.listFiles();
	    
	    //Check if any files in directory
	    if( imgFiles.length > 0 )
	    	//Go through all of them
	    	for(int i = 0; i < imgFiles.length; i++)
	    		//Delete file
	    		imgFiles[ i ].delete(); 
	}
	
	public static void setVisibility(boolean visible){
		frame.setVisible(visible);
	}
	
	
	public static void setChosenGif(int buttonNumber){
		chosenGif = buttonNumber;
	}
	
	public static int getChosenGif(){
		return chosenGif;
	}
	

}
