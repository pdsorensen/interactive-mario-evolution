package iec;

import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;

public class ButtonListener implements ActionListener {

	int buttonNumber;
	
	public ButtonListener (int buttonNumber){
		this.buttonNumber = buttonNumber;
	}
	
	public void actionPerformed(ActionEvent e) {
		MarioGIF.setChosenGif(buttonNumber);
		//System.exit(0);
	}

	public int actionPerformed(){
		return buttonNumber;
	}
	
}
