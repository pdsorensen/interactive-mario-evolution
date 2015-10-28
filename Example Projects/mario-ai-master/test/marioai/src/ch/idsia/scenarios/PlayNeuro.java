package ch.idsia.scenarios;

import ch.idsia.ai.agents.Agent;
import ch.idsia.ai.agents.AgentsPool;
import ch.idsia.ai.agents.ai.NeuroAgent;
import ch.idsia.ai.agents.ai.TimingAgent;
import ch.idsia.ai.agents.human.HumanKeyboardAgent;
import ch.idsia.ai.tasks.ProgressTask;
import ch.idsia.ai.tasks.Task;
import ch.idsia.tools.CmdLineOptions;
import ch.idsia.tools.EvaluationOptions;
import ch.idsia.tools.EvaluationInfo;
import ch.idsia.tools.Evaluator;
import ch.idsia.tools.tcp.Server;
import ch.idsia.utils.StatisticalSummary;
import ch.idsia.mario.engine.sprites.Mario;

import java.util.List;

/**
 * Created by IntelliJ IDEA.
 * User: J.A.Thompson, esq.
 * Date: May 5, 2009
 * Time: 12:46:43 PM
 */
public class PlayNeuro {

    private static int killsSum = 0;
    private static int marioStatusSum = 0;
    private static int timeLeftSum = 0;
    private static int marioModeSum = 0;
    private static int portNumber = -1;
    private static int runCount = 0;
    private static String parameters = "";
    private static boolean keepLooping = true;

    public static void main(String[] args) {
        System.out.println("Welcome to Infinite Luigi Bros!");
        EvaluationOptions evaluationOptions = HandleArguments(args);  // if none options mentioned, all defaults are used.
        if(portNumber == -1)
            System.out.println("ERROR: Never received a port number as an argument!");
        Server server = new Server(portNumber, 1, 11);
        Agent controller = new NeuroAgent(server);

        while(keepLooping)
        {
            evaluationOptions = HandleArguments(new String[]{server.recvUnSafe()});
            evaluationOptions.setAgent(controller);
            Evaluator evaluator = new Evaluator(evaluationOptions);
            List<EvaluationInfo> evaluationSummary = evaluator.evaluate();
            server.sendSafe(CreateScoreString(evaluationSummary.get(0)));
        }

        //server.ShutDownServer();
        //System.exit(0);
    }

    public static String CreateScoreString(EvaluationInfo results)
    {
        String scoreString = "Score:Distance=";
        scoreString += results.computeDistancePassed();
        scoreString += ";Time=" + results.timeLeft;
        scoreString += ";Coin=" + results.numberOfGainedCoins;
        scoreString += ";LuigiSize=" + results.marioMode;
        scoreString += ";Kills=" + results.computeKillsTotal();
        scoreString += ";Powerups=" + results.computeTotalPowerups();
        scoreString += ";Bricks=" + results.blocksHit;
        scoreString += ";Success=" + (results.marioStatus == Mario.STATUS_WIN ? "1" : "0");
        scoreString += ";Suicide=" + (results.marioStatus == Mario.STATUS_SUICIDE ? "1" : "0");
        scoreString += ";Jumps=" + results.totalJumpsPerformed;
        return scoreString;
    }

    public static EvaluationOptions HandleArguments(String[] args)
    {
        EvaluationOptions evalOptions = new EvaluationOptions();
        String[] simulationArguments = args;
        if(args.length == 1)
        {
            if(!args[0].equals(parameters))
            {
                if(runCount == 1)
                {
                    parameters = args[0];
                    simulationArguments = parameters.split(" ");
                }
                else
                {
                    String[] old = parameters.split(" ");
                    parameters = args[0];
                    simulationArguments = parameters.split(" ");
                    // JAT uncomment for argument change notification
                    //for(int i = 0; i < simulationArguments.length && i < old.length; i++)
                    //{
                        //if(!old[i].equals(simulationArguments[i]))
                        //{
                          //  System.out.println(old[i] + " --> " + simulationArguments[i]);
                        //}
                    //}
                }
            }
            else
                simulationArguments = parameters.split(" ");
        }
        else
            simulationArguments = args;
        System.out.println("Run number: " + runCount++);
        for(int i = 0; i < simulationArguments.length; i++)
        {
            String[] splitSplitArgs = simulationArguments[i].split(":");
            // port is a special case since we're not using ServerAgent -port command
            // maxFPS is also a special case since it's not technically part of any option set
            if(splitSplitArgs[0].equals("-port"))
                portNumber = Integer.parseInt(splitSplitArgs[1]);
            else if(splitSplitArgs[0].equals("-maxFPS"))
                evalOptions.setMaxFPS(splitSplitArgs[1].equals("on") ? true : false);
            else
                evalOptions.setParameterValue(splitSplitArgs[0], splitSplitArgs[1]);
        }
        return evalOptions;
    }

    
}