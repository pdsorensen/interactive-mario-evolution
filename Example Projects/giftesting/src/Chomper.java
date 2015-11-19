import java.awt.*;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.net.URL;
import javax.imageio.ImageIO;
import javax.swing.*;

class Chomper {

    public static void main(String[] args) throws Exception {
        final Image[] frames = {
            ImageIO.read(new URL("http://i.stack.imgur.com/XUmOD.png")),
            ImageIO.read(new URL("http://i.stack.imgur.com/zKyiD.png")),
            ImageIO.read(new URL("http://i.stack.imgur.com/4maMm.png")),
            ImageIO.read(new URL("http://i.stack.imgur.com/wn9V5.png"))
        };
        Runnable r = new Runnable() {

            @Override
            public void run() {
                JPanel gui = new JPanel(new BorderLayout());

                final JLabel animation = new JLabel(new ImageIcon(frames[0]));
                gui.add(animation, BorderLayout.CENTER);

                ActionListener animate = new ActionListener() {

                    private int index = 0;

                    @Override
                    public void actionPerformed(ActionEvent e) {
                        if (index<frames.length-1) {
                            index++;
                        } else {
                            index = 0;
                        }
                        animation.setIcon(new ImageIcon(frames[index]));
                    }
                };
                final Timer timer = new Timer(200,animate);

                final JToggleButton b = new JToggleButton("Start/Stop");
                ActionListener startStop = new ActionListener() {

                    @Override
                    public void actionPerformed(ActionEvent e) {
                        if (b.isSelected()) {
                            timer.start();
                        } else {
                            timer.stop();
                        }
                    }
                };
                b.addActionListener(startStop);
                gui.add(b, BorderLayout.PAGE_END);

                JOptionPane.showMessageDialog(null, gui);
            }
        };
        // Swing GUIs should be created and updated on the EDT
        // http://docs.oracle.com/javase/tutorial/uiswing/concurrency/initial.html
        SwingUtilities.invokeLater(r);
    }
}