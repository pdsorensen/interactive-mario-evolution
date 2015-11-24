package iec;

import java.awt.BorderLayout;
import java.awt.Dimension;
import java.awt.FlowLayout;
import java.awt.GridLayout;

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
	public static void runIEC(){
		Runnable r = new Runnable() {
			@Override
			public void run() {
				
				//Frame parameters
				JFrame frame = new JFrame(); 
				//JLabel headerLabel = new JLabel("headerLabel",JLabel.CENTER );
				JPanel contentPane = new JPanel();
				
				
	            frame.setSize(new Dimension(1600, 1000));
	            frame.setTitle("Mario AI Evaluator");
	            frame.setLayout(new GridLayout(0,1));
	           
	            
				// Get the GIFS into ImageIcons
	            contentPane.setLayout(new GridLayout(0,3));
				ImageIcon[] gifs = new ImageIcon[9];
				for(int i = 0; i<gifs.length;i++){
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
	
	public static void main(String[] args) throws Exception {
		runIEC();
		
	}
}
