import java.awt.BorderLayout;
import java.awt.GridLayout;
import javax.swing.JButton;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.SwingConstants;

public class marioGRID extends JFrame
{
private JPanel pan;
public marioGRID()
{

    GridLayout g=new GridLayout(3,3);
    pan=new JPanel(g);
    pan.add(new JButton("1"));
    pan.add(new JButton("2"));
    pan.add(new JButton("3"));
    pan.add(new JButton("4"));
    pan.add(new JButton("5"));
    pan.add(new JButton("6"));
    pan.add(new JButton("7"));
    pan.add(new JButton("8"));
    pan.add(new JButton("9"));
    JLabel l=new JLabel("grid layout");
    l.setHorizontalAlignment(SwingConstants.CENTER);


    setLayout(new BorderLayout());
    add(l,BorderLayout.NORTH);
    add(pan,BorderLayout.CENTER);
    setSize(1000,500);
    setVisible(true);




}
public static void main(String args[])
{
    new marioGRID();
}

}