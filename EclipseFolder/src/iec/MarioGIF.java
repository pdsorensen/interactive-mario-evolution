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


public class MarioGIF {
	
	static int chosenGif;
	public static int iecGeneration = 1; 
	
	public static void runIEC(){
		
		Runnable r = new Runnable() {
			@Override
			public void run() {
				
				chosenGif = -1;
				
				//Frame parameters
				JFrame frame = new JFrame(); 
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
					String fileLocation = "./db/gifs/" + new Integer(i).toString() + ".gif";
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
	
	public static void deleteGifs(){
		final File dir = new File("./db/gifs/");
	    ArrayList<BufferedImage> images = new ArrayList<BufferedImage>(); 
	    File[] imgFiles = dir.listFiles();
	    for(int i = 0; i < 9; i++)
	    	imgFiles[ i ].delete(); 
	}
	
	public static void setChosenGif(int buttonNumber){
		chosenGif = buttonNumber;
	}
	
	public static int getChosenGif(){
		return chosenGif;
	}
	

}
