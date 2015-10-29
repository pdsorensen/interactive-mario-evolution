using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Data;
using System.Threading;
using System.Xml;
using System.Globalization;
using System.Runtime.Remoting;

using SharpNeatLib;
using SharpNeatLib.AppConfig;
using SharpNeatLib.Evolution;
using SharpNeatLib.Evolution.Xml;
using SharpNeatLib.Experiments;
using SharpNeatLib.NeatGenome;
using SharpNeatLib.NeatGenome.Xml;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.NeuralNetwork.Xml;
using SharpNeatLib.Xml;

using SharpNeatExperiments.Luigi;

namespace SharpNeat
{
	public class Form1 : System.Windows.Forms.Form
	{
		delegate void MessageDelegate(string message);
        LuigiParameters luigiParameters = new LuigiParameters();

        bool IsLuigiExperiment { get { return TheLuigiExperiment != null; } }
        LuigiExperiment TheLuigiExperiment { get { return selectedExperiment as LuigiExperiment; } }

		#region Enumerations

		enum SearchStateEnum
		{
			Reset,
			Paused,
			Running
		}

		#endregion

		#region Class Variables

		SearchStateEnum searchState = SearchStateEnum.Reset;
		
		ExperimentConfigInfo[] experimentConfigInfoArray = null;
		IExperiment selectedExperiment=null;
		ExperimentConfigInfo selectedExperimentConfigInfo = null;
		
		IActivationFunction selectedActivationFunction=null;
		EvolutionAlgorithm ea;
		Population pop;
		Thread searchThread;
		long ticksAtSearchStart;
		NumberFormatInfo nfi;

		int evaluationsPerSec;

		bool stopSearchSignal;


		BestGenomeForm bestGenomeForm=null;
		SpeciesForm speciesForm=null;
		AbstractExperimentView experimentView=null;
		ProgressForm progressForm=null;
		ActivationFunctionForm activationFunctionForm=null;

		StreamWriter logWriter = null;

		object guiThreadLockObject = new object();

		
		/// <summary>
		/// Update Frequency (Generations).
		/// </summary>
		ulong updateFreqGens=1;

		/// <summary>
		/// Update Frequency (100 nanosecond ticks).
		/// </summary>
		long updateFreqTicks=10000000; // 1 second.

		/// <summary>
		/// Update mode. false=generations, true=seconds.
		/// </summary>
		bool updateMode = true;

		#endregion

		#region Windows Form Designer Variables

        private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.GroupBox gbxLog;
		private System.Windows.Forms.TextBox txtLogWindow;
		private System.Windows.Forms.GroupBox gbxCurrentStats;
		private System.Windows.Forms.TextBox txtStatsBest;
		private System.Windows.Forms.TextBox txtStatsMean;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox txtStatsGeneration;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox txtStatsSpeciesCount;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox txtStatsCompatibilityThreshold;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox txtStatsTotalEvaluations;
		private System.Windows.Forms.Label label27;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem mnuAbout;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtDomainOutputNeuronCount;
		private System.Windows.Forms.TextBox txtDomainInputNeuronCount;
		private System.Windows.Forms.ComboBox cmbDomain;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.Label label29;
		private System.Windows.Forms.TextBox txtParamSpeciesDropoffAge;
		private System.Windows.Forms.Label label30;
		private System.Windows.Forms.TextBox txtParamTargetSpeciesCountMax;
		private System.Windows.Forms.Label label31;
		private System.Windows.Forms.TextBox txtParamTargetSpeciesCountMin;
		private System.Windows.Forms.Label label32;
		private System.Windows.Forms.TextBox txtParamSelectionProportion;
		private System.Windows.Forms.Label label33;
		private System.Windows.Forms.TextBox txtParamElitismProportion;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label34;
		private System.Windows.Forms.TextBox txtParamMutateConnectionWeights;
		private System.Windows.Forms.Label label35;
		private System.Windows.Forms.TextBox txtParamMutateAddNode;
		private System.Windows.Forms.Label label36;
		private System.Windows.Forms.TextBox txtParamMutateAddConnection;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.TextBox txtParamCompatDisjointCoeff;
		private System.Windows.Forms.Label label22;
		private System.Windows.Forms.TextBox txtParamCompatExcessCoeff;
		private System.Windows.Forms.Label label23;
		private System.Windows.Forms.TextBox txtParamCompatWeightDeltaCoeff;
		private System.Windows.Forms.Label label24;
		private System.Windows.Forms.TextBox txtParamCompatThreshold;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.TextBox txtParamOffspringCrossover;
		private System.Windows.Forms.Label label26;
		private System.Windows.Forms.TextBox txtParamOffspringMutation;
		private System.Windows.Forms.Label label28;
		private System.Windows.Forms.TextBox txtParamPopulationSize;
		private System.Windows.Forms.GroupBox gbxSearchParameters;
		private System.Windows.Forms.GroupBox gbxFile;
		private System.Windows.Forms.TextBox txtFileBaseName;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.Button btnSearchStart;
		private System.Windows.Forms.Button btnSearchStop;
		private System.Windows.Forms.Button btnSearchReset;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.TextBox txtParamInterspeciesMating;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.MenuItem mnuFileSaveBestAsNetwork;
		private System.Windows.Forms.MenuItem mnuFileSaveBestAsGenome;
		private System.Windows.Forms.MenuItem mnuFileSavePopulation;
		private System.Windows.Forms.MenuItem mnuInitPopLoad;
		private System.Windows.Forms.MenuItem mnuInitPopLoadPopulation;
		private System.Windows.Forms.MenuItem mnuInitPopLoadSeedGenome;
		private System.Windows.Forms.MenuItem mnuInitPopLoadSeedPopulation;
		private System.Windows.Forms.MenuItem mnuInitPop;
		private System.Windows.Forms.MenuItem mnuInitPopAutoGenerate;
		private System.Windows.Forms.Button btnExperimentInfo;
		private System.Windows.Forms.Button btnLoadDefaults;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.TextBox txtParamConnectionWeightMutationSigma;
		private System.Windows.Forms.TextBox txtParamConnectionWeightRange;
		private System.Windows.Forms.TextBox txtStatsBestGenomeLength;
		private System.Windows.Forms.TextBox txtStatsMeanGenomeLength;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.Label label19;
		private System.Windows.Forms.CheckBox chkFileSaveGenomeOnImprovement;
		private System.Windows.Forms.TextBox txtStatsEvaluationsPerSec;
		private System.Windows.Forms.Label label20;
		private System.Windows.Forms.MenuItem mnuFileSave;
		private System.Windows.Forms.Label label21;
		private System.Windows.Forms.ComboBox cmbExperimentActivationFn;
		private System.Windows.Forms.TextBox txtExperimentActivationFn;
		private System.Windows.Forms.MenuItem mnuVisualization;
		private System.Windows.Forms.MenuItem mnuVisualizationBest;
		private System.Windows.Forms.MenuItem mnuVisualizationSpecies;
		private System.Windows.Forms.MenuItem mnuVisualizationExperiment;
		private System.Windows.Forms.Label label37;
		private System.Windows.Forms.TextBox txtFileLogBaseName;
		private System.Windows.Forms.CheckBox chkFileWriteLog;
		private System.Windows.Forms.Label label38;
		private System.Windows.Forms.TextBox txtStatsMode;
		private System.Windows.Forms.CheckBox chkParamPruningModeEnabled;
		private System.Windows.Forms.Label label39;
		private System.Windows.Forms.Label label40;
		private System.Windows.Forms.CheckBox chkParamEnableConnectionWeightFixing;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.GroupBox groupBox7;
		private System.Windows.Forms.TextBox txtParamPruningBeginFitnessStagnationThreshold;
		private System.Windows.Forms.TextBox txtParamPruningBeginComplexityThreshold;
		private System.Windows.Forms.TextBox txtParamPruningEndComplexityStagnationThreshold;
		private System.Windows.Forms.Label label41;
		private System.Windows.Forms.Label label42;
		private System.Windows.Forms.Label label43;
		private System.Windows.Forms.TextBox txtParamMutateDeleteConnection;
		private System.Windows.Forms.TextBox txtParamMutateDeleteNeuron;
		private System.Windows.Forms.MenuItem mnuView;
		private System.Windows.Forms.MenuItem mnuViewUpdateFrequency;
		private System.Windows.Forms.MenuItem mnuViewUpdateFrequency1Sec;
		private System.Windows.Forms.MenuItem mnuViewUpdateFrequency2Sec;
		private System.Windows.Forms.MenuItem mnuViewUpdateFrequency5Sec;
		private System.Windows.Forms.MenuItem mnuViewUpdateFrequency10Sec;
		private System.Windows.Forms.MenuItem mnuViewUpdateFrequency1Gen;
		private System.Windows.Forms.MenuItem mnuViewUpdateFrequency2Gen;
		private System.Windows.Forms.MenuItem mnuViewUpdateFrequency5Gen;
		private System.Windows.Forms.MenuItem mnuViewUpdateFrequency10Gen;
		private System.Windows.Forms.TextBox txtStatsEvaluatorStateMsg;
		private System.Windows.Forms.MenuItem mnuVisualizationProgressGraph;
        private System.Windows.Forms.Button btnPlotFunction;

		#endregion
        private GroupBox groupBox8;
        private Label label11;
        private TextBox luigi_PortNumber;
        private CheckBox luigi_enableGameViewer;
        private CheckBox luigi_MaxFPS;
        private GroupBox groupBox10;
        private Label label9;
        private TextBox luigi_LevelDifficulty;
        private Panel panel3;
        private RadioButton luigi_LuigiFire;
        private RadioButton luigi_LuigiSmall;
        private RadioButton luigi_LuigiBig;
        private CheckBox luigi_StopSimAfterFirstWin;
        private Panel panel4;
        private RadioButton luigi_Random;
        private RadioButton luigi_Castle;
        private RadioButton luigi_Overground;
        private RadioButton luigi_Underground;
        private Label label44;
        private TextBox luigi_TimeLimit;
        private Label label45;
        private TextBox luigi_LevelLength;
        private CheckBox luigi_UseRandomSeed;
        private CheckBox luigi_EnableVisualization;
        private Label label46;
        private TextBox luigi_LevelRandomizationSeed;
        private CheckBox luigi_RandomPort;
        private Label label48;
        private TextBox luigi_LevelRandomizationSeedMax;
        private TextBox luigi_LevelRandomizationSeedMin;
        private Label label47;
        private TextBox luigi_RandomPortMax;
        private TextBox luigi_RandomPortMin;
        private GroupBox groupBox9;
        private Label label52;
        private TextBox luigi_FitnessTime;
        private Label label51;
        private TextBox luigi_FitnessDistance;
        private Label label10;
        private TextBox luigi_NumberOfRuns;
        private Label label55;
        private TextBox luigi_FitnessKills;
        private Label label53;
        private TextBox luigi_FitnessLuigiSize;
        private Label label54;
        private TextBox luigi_FitnessCoin;
        private Label label50;
        private TextBox luigi_FitnessStopThreshold;
        private Label label56;
        private TextBox luigi_FitnessPowerups;
        private CheckBox luigi_UseGenerationStopThreshold;
        private CheckBox luigi_FitnessUseStopThreshold;
        private Label label57;
        private TextBox luigi_GenerationStopThreshold;
        private Label label58;
        private Label label49;
        private Panel panel6;
        private RadioButton luigi_EnemyZ2;
        private RadioButton luigi_EnemyZ0;
        private RadioButton luigi_EnemyZ1;
        private Panel panel5;
        private RadioButton luigi_MapZ2;
        private RadioButton luigi_MapZ0;
        private RadioButton luigi_MapZ1;
        private Label label61;
        private Label label60;
        private Label label59;
        private Splitter splitter1;
        private Label label63;
        private TextBox luigi_FitnessSuicidePenalty;
        private Label label62;
        private TextBox luigi_FitnessVictoryBonus;
        private Label label64;
        private TextBox luigi_FitnessBricks;
        private Panel panel7;
        private RadioButton luigi_UseFullJumpScript;
        private RadioButton luigi_UseNoJumpScript;
        private RadioButton luigi_UseHybridJumpScript;
        private Label label65;
        private TextBox luigi_FitnessJumpPenalty;
        private CheckBox luigi_UseInverseDistances;
        private CheckBox luigi_VictorySound;
        private TextBox luigi_IncreaseLengthAmount;
        private TextBox luigi_IncreaseDifficultyAmount;
        private CheckBox luigi_IncreaseLength;
        private CheckBox luigi_IncreaseDifficulty;
        private IContainer components;

		#region Constructor / Disposal

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			InitialiseForm();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
	
		#endregion
			
		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.btnPlotFunction = new System.Windows.Forms.Button();
            this.txtExperimentActivationFn = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.cmbExperimentActivationFn = new System.Windows.Forms.ComboBox();
            this.btnLoadDefaults = new System.Windows.Forms.Button();
            this.btnExperimentInfo = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtDomainOutputNeuronCount = new System.Windows.Forms.TextBox();
            this.txtDomainInputNeuronCount = new System.Windows.Forms.TextBox();
            this.cmbDomain = new System.Windows.Forms.ComboBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.btnSearchReset = new System.Windows.Forms.Button();
            this.btnSearchStop = new System.Windows.Forms.Button();
            this.btnSearchStart = new System.Windows.Forms.Button();
            this.gbxFile = new System.Windows.Forms.GroupBox();
            this.label37 = new System.Windows.Forms.Label();
            this.txtFileLogBaseName = new System.Windows.Forms.TextBox();
            this.chkFileWriteLog = new System.Windows.Forms.CheckBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtFileBaseName = new System.Windows.Forms.TextBox();
            this.chkFileSaveGenomeOnImprovement = new System.Windows.Forms.CheckBox();
            this.gbxCurrentStats = new System.Windows.Forms.GroupBox();
            this.label38 = new System.Windows.Forms.Label();
            this.txtStatsMode = new System.Windows.Forms.TextBox();
            this.txtStatsEvaluationsPerSec = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.txtStatsMeanGenomeLength = new System.Windows.Forms.TextBox();
            this.txtStatsBestGenomeLength = new System.Windows.Forms.TextBox();
            this.txtStatsTotalEvaluations = new System.Windows.Forms.TextBox();
            this.label27 = new System.Windows.Forms.Label();
            this.txtStatsEvaluatorStateMsg = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtStatsCompatibilityThreshold = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtStatsGeneration = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtStatsSpeciesCount = new System.Windows.Forms.TextBox();
            this.txtStatsMean = new System.Windows.Forms.TextBox();
            this.txtStatsBest = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.gbxSearchParameters = new System.Windows.Forms.GroupBox();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.label41 = new System.Windows.Forms.Label();
            this.txtParamPruningEndComplexityStagnationThreshold = new System.Windows.Forms.TextBox();
            this.label40 = new System.Windows.Forms.Label();
            this.txtParamPruningBeginFitnessStagnationThreshold = new System.Windows.Forms.TextBox();
            this.label39 = new System.Windows.Forms.Label();
            this.txtParamPruningBeginComplexityThreshold = new System.Windows.Forms.TextBox();
            this.chkParamEnableConnectionWeightFixing = new System.Windows.Forms.CheckBox();
            this.chkParamPruningModeEnabled = new System.Windows.Forms.CheckBox();
            this.txtParamPopulationSize = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.txtParamConnectionWeightMutationSigma = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.txtParamConnectionWeightRange = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label29 = new System.Windows.Forms.Label();
            this.txtParamSpeciesDropoffAge = new System.Windows.Forms.TextBox();
            this.label30 = new System.Windows.Forms.Label();
            this.txtParamTargetSpeciesCountMax = new System.Windows.Forms.TextBox();
            this.label31 = new System.Windows.Forms.Label();
            this.txtParamTargetSpeciesCountMin = new System.Windows.Forms.TextBox();
            this.label32 = new System.Windows.Forms.Label();
            this.txtParamSelectionProportion = new System.Windows.Forms.TextBox();
            this.label33 = new System.Windows.Forms.Label();
            this.txtParamElitismProportion = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label43 = new System.Windows.Forms.Label();
            this.txtParamMutateDeleteNeuron = new System.Windows.Forms.TextBox();
            this.label42 = new System.Windows.Forms.Label();
            this.txtParamMutateDeleteConnection = new System.Windows.Forms.TextBox();
            this.label34 = new System.Windows.Forms.Label();
            this.txtParamMutateConnectionWeights = new System.Windows.Forms.TextBox();
            this.label35 = new System.Windows.Forms.Label();
            this.txtParamMutateAddNode = new System.Windows.Forms.TextBox();
            this.label36 = new System.Windows.Forms.Label();
            this.txtParamMutateAddConnection = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtParamCompatDisjointCoeff = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.txtParamCompatExcessCoeff = new System.Windows.Forms.TextBox();
            this.label23 = new System.Windows.Forms.Label();
            this.txtParamCompatWeightDeltaCoeff = new System.Windows.Forms.TextBox();
            this.label24 = new System.Windows.Forms.Label();
            this.txtParamCompatThreshold = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label15 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.txtParamInterspeciesMating = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.txtParamOffspringCrossover = new System.Windows.Forms.TextBox();
            this.label26 = new System.Windows.Forms.Label();
            this.txtParamOffspringMutation = new System.Windows.Forms.TextBox();
            this.label28 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.label65 = new System.Windows.Forms.Label();
            this.luigi_FitnessJumpPenalty = new System.Windows.Forms.TextBox();
            this.label64 = new System.Windows.Forms.Label();
            this.luigi_FitnessBricks = new System.Windows.Forms.TextBox();
            this.label63 = new System.Windows.Forms.Label();
            this.luigi_FitnessSuicidePenalty = new System.Windows.Forms.TextBox();
            this.label62 = new System.Windows.Forms.Label();
            this.luigi_FitnessVictoryBonus = new System.Windows.Forms.TextBox();
            this.luigi_UseGenerationStopThreshold = new System.Windows.Forms.CheckBox();
            this.luigi_FitnessUseStopThreshold = new System.Windows.Forms.CheckBox();
            this.label57 = new System.Windows.Forms.Label();
            this.luigi_GenerationStopThreshold = new System.Windows.Forms.TextBox();
            this.label50 = new System.Windows.Forms.Label();
            this.luigi_FitnessStopThreshold = new System.Windows.Forms.TextBox();
            this.label56 = new System.Windows.Forms.Label();
            this.luigi_FitnessPowerups = new System.Windows.Forms.TextBox();
            this.label55 = new System.Windows.Forms.Label();
            this.luigi_FitnessKills = new System.Windows.Forms.TextBox();
            this.label53 = new System.Windows.Forms.Label();
            this.luigi_FitnessLuigiSize = new System.Windows.Forms.TextBox();
            this.label54 = new System.Windows.Forms.Label();
            this.luigi_FitnessCoin = new System.Windows.Forms.TextBox();
            this.label52 = new System.Windows.Forms.Label();
            this.luigi_FitnessTime = new System.Windows.Forms.TextBox();
            this.label51 = new System.Windows.Forms.Label();
            this.luigi_FitnessDistance = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.luigi_NumberOfRuns = new System.Windows.Forms.TextBox();
            this.groupBox10 = new System.Windows.Forms.GroupBox();
            this.luigi_VictorySound = new System.Windows.Forms.CheckBox();
            this.panel7 = new System.Windows.Forms.Panel();
            this.luigi_UseFullJumpScript = new System.Windows.Forms.RadioButton();
            this.luigi_UseNoJumpScript = new System.Windows.Forms.RadioButton();
            this.luigi_UseHybridJumpScript = new System.Windows.Forms.RadioButton();
            this.luigi_UseInverseDistances = new System.Windows.Forms.CheckBox();
            this.label61 = new System.Windows.Forms.Label();
            this.label60 = new System.Windows.Forms.Label();
            this.label59 = new System.Windows.Forms.Label();
            this.label58 = new System.Windows.Forms.Label();
            this.label49 = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.luigi_EnemyZ2 = new System.Windows.Forms.RadioButton();
            this.luigi_EnemyZ0 = new System.Windows.Forms.RadioButton();
            this.luigi_EnemyZ1 = new System.Windows.Forms.RadioButton();
            this.panel5 = new System.Windows.Forms.Panel();
            this.luigi_MapZ2 = new System.Windows.Forms.RadioButton();
            this.luigi_MapZ0 = new System.Windows.Forms.RadioButton();
            this.luigi_MapZ1 = new System.Windows.Forms.RadioButton();
            this.label48 = new System.Windows.Forms.Label();
            this.luigi_LevelRandomizationSeedMax = new System.Windows.Forms.TextBox();
            this.luigi_LevelRandomizationSeedMin = new System.Windows.Forms.TextBox();
            this.luigi_UseRandomSeed = new System.Windows.Forms.CheckBox();
            this.luigi_EnableVisualization = new System.Windows.Forms.CheckBox();
            this.label46 = new System.Windows.Forms.Label();
            this.luigi_enableGameViewer = new System.Windows.Forms.CheckBox();
            this.luigi_LevelRandomizationSeed = new System.Windows.Forms.TextBox();
            this.luigi_MaxFPS = new System.Windows.Forms.CheckBox();
            this.label45 = new System.Windows.Forms.Label();
            this.luigi_LevelLength = new System.Windows.Forms.TextBox();
            this.panel4 = new System.Windows.Forms.Panel();
            this.luigi_Random = new System.Windows.Forms.RadioButton();
            this.luigi_Castle = new System.Windows.Forms.RadioButton();
            this.luigi_Overground = new System.Windows.Forms.RadioButton();
            this.luigi_Underground = new System.Windows.Forms.RadioButton();
            this.label44 = new System.Windows.Forms.Label();
            this.luigi_TimeLimit = new System.Windows.Forms.TextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.luigi_LuigiFire = new System.Windows.Forms.RadioButton();
            this.luigi_LuigiSmall = new System.Windows.Forms.RadioButton();
            this.luigi_LuigiBig = new System.Windows.Forms.RadioButton();
            this.luigi_StopSimAfterFirstWin = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.luigi_LevelDifficulty = new System.Windows.Forms.TextBox();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.label47 = new System.Windows.Forms.Label();
            this.luigi_RandomPortMax = new System.Windows.Forms.TextBox();
            this.luigi_RandomPortMin = new System.Windows.Forms.TextBox();
            this.luigi_RandomPort = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.luigi_PortNumber = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.gbxLog = new System.Windows.Forms.GroupBox();
            this.txtLogWindow = new System.Windows.Forms.TextBox();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.mnuFileSave = new System.Windows.Forms.MenuItem();
            this.mnuFileSavePopulation = new System.Windows.Forms.MenuItem();
            this.mnuFileSaveBestAsNetwork = new System.Windows.Forms.MenuItem();
            this.mnuFileSaveBestAsGenome = new System.Windows.Forms.MenuItem();
            this.mnuInitPop = new System.Windows.Forms.MenuItem();
            this.mnuInitPopLoad = new System.Windows.Forms.MenuItem();
            this.mnuInitPopLoadPopulation = new System.Windows.Forms.MenuItem();
            this.mnuInitPopLoadSeedGenome = new System.Windows.Forms.MenuItem();
            this.mnuInitPopLoadSeedPopulation = new System.Windows.Forms.MenuItem();
            this.mnuInitPopAutoGenerate = new System.Windows.Forms.MenuItem();
            this.mnuView = new System.Windows.Forms.MenuItem();
            this.mnuViewUpdateFrequency = new System.Windows.Forms.MenuItem();
            this.mnuViewUpdateFrequency1Sec = new System.Windows.Forms.MenuItem();
            this.mnuViewUpdateFrequency2Sec = new System.Windows.Forms.MenuItem();
            this.mnuViewUpdateFrequency5Sec = new System.Windows.Forms.MenuItem();
            this.mnuViewUpdateFrequency10Sec = new System.Windows.Forms.MenuItem();
            this.mnuViewUpdateFrequency1Gen = new System.Windows.Forms.MenuItem();
            this.mnuViewUpdateFrequency2Gen = new System.Windows.Forms.MenuItem();
            this.mnuViewUpdateFrequency5Gen = new System.Windows.Forms.MenuItem();
            this.mnuViewUpdateFrequency10Gen = new System.Windows.Forms.MenuItem();
            this.mnuVisualization = new System.Windows.Forms.MenuItem();
            this.mnuVisualizationProgressGraph = new System.Windows.Forms.MenuItem();
            this.mnuVisualizationBest = new System.Windows.Forms.MenuItem();
            this.mnuVisualizationSpecies = new System.Windows.Forms.MenuItem();
            this.mnuVisualizationExperiment = new System.Windows.Forms.MenuItem();
            this.mnuAbout = new System.Windows.Forms.MenuItem();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.luigi_IncreaseDifficulty = new System.Windows.Forms.CheckBox();
            this.luigi_IncreaseLength = new System.Windows.Forms.CheckBox();
            this.luigi_IncreaseDifficultyAmount = new System.Windows.Forms.TextBox();
            this.luigi_IncreaseLengthAmount = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.gbxFile.SuspendLayout();
            this.gbxCurrentStats.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.gbxSearchParameters.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.groupBox9.SuspendLayout();
            this.groupBox10.SuspendLayout();
            this.panel7.SuspendLayout();
            this.panel6.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel3.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.panel2.SuspendLayout();
            this.gbxLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tabControl1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(467, 550);
            this.panel1.TabIndex = 4;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(467, 550);
            this.tabControl1.TabIndex = 18;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox5);
            this.tabPage1.Controls.Add(this.groupBox6);
            this.tabPage1.Controls.Add(this.gbxFile);
            this.tabPage1.Controls.Add(this.gbxCurrentStats);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(459, 514);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Page 1";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.btnPlotFunction);
            this.groupBox5.Controls.Add(this.txtExperimentActivationFn);
            this.groupBox5.Controls.Add(this.label21);
            this.groupBox5.Controls.Add(this.cmbExperimentActivationFn);
            this.groupBox5.Controls.Add(this.btnLoadDefaults);
            this.groupBox5.Controls.Add(this.btnExperimentInfo);
            this.groupBox5.Controls.Add(this.label8);
            this.groupBox5.Controls.Add(this.label1);
            this.groupBox5.Controls.Add(this.txtDomainOutputNeuronCount);
            this.groupBox5.Controls.Add(this.txtDomainInputNeuronCount);
            this.groupBox5.Controls.Add(this.cmbDomain);
            this.groupBox5.Location = new System.Drawing.Point(0, 0);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(224, 240);
            this.groupBox5.TabIndex = 14;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Domain / Experiment";
            // 
            // btnPlotFunction
            // 
            this.btnPlotFunction.Font = new System.Drawing.Font("Symbol", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.btnPlotFunction.Location = new System.Drawing.Point(200, 88);
            this.btnPlotFunction.Name = "btnPlotFunction";
            this.btnPlotFunction.Size = new System.Drawing.Size(19, 21);
            this.btnPlotFunction.TabIndex = 52;
            this.btnPlotFunction.Text = "¼";
            this.btnPlotFunction.Click += new System.EventHandler(this.btnPlotFunction_Click);
            // 
            // txtExperimentActivationFn
            // 
            this.txtExperimentActivationFn.Location = new System.Drawing.Point(8, 120);
            this.txtExperimentActivationFn.Name = "txtExperimentActivationFn";
            this.txtExperimentActivationFn.ReadOnly = true;
            this.txtExperimentActivationFn.Size = new System.Drawing.Size(208, 20);
            this.txtExperimentActivationFn.TabIndex = 51;
            // 
            // label21
            // 
            this.label21.Location = new System.Drawing.Point(8, 72);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(120, 12);
            this.label21.TabIndex = 50;
            this.label21.Text = "Activation Fn";
            this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbExperimentActivationFn
            // 
            this.cmbExperimentActivationFn.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExperimentActivationFn.Location = new System.Drawing.Point(8, 88);
            this.cmbExperimentActivationFn.Name = "cmbExperimentActivationFn";
            this.cmbExperimentActivationFn.Size = new System.Drawing.Size(192, 21);
            this.cmbExperimentActivationFn.TabIndex = 49;
            this.cmbExperimentActivationFn.SelectedIndexChanged += new System.EventHandler(this.cmbExperimentActivationFn_SelectedIndexChanged);
            // 
            // btnLoadDefaults
            // 
            this.btnLoadDefaults.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLoadDefaults.Location = new System.Drawing.Point(8, 40);
            this.btnLoadDefaults.Name = "btnLoadDefaults";
            this.btnLoadDefaults.Size = new System.Drawing.Size(192, 24);
            this.btnLoadDefaults.TabIndex = 48;
            this.btnLoadDefaults.Text = "Load Default Search Parameters";
            this.btnLoadDefaults.Click += new System.EventHandler(this.btnLoadDefaults_Click);
            // 
            // btnExperimentInfo
            // 
            this.btnExperimentInfo.Font = new System.Drawing.Font("Arial Black", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExperimentInfo.Location = new System.Drawing.Point(200, 16);
            this.btnExperimentInfo.Name = "btnExperimentInfo";
            this.btnExperimentInfo.Size = new System.Drawing.Size(19, 21);
            this.btnExperimentInfo.TabIndex = 47;
            this.btnExperimentInfo.Text = "?";
            this.btnExperimentInfo.Click += new System.EventHandler(this.btnDomainExplanation_Click);
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(64, 168);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(88, 16);
            this.label8.TabIndex = 43;
            this.label8.Text = "Output Neurons";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(64, 144);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 16);
            this.label1.TabIndex = 42;
            this.label1.Text = "Input Neurons";
            // 
            // txtDomainOutputNeuronCount
            // 
            this.txtDomainOutputNeuronCount.Location = new System.Drawing.Point(8, 168);
            this.txtDomainOutputNeuronCount.Name = "txtDomainOutputNeuronCount";
            this.txtDomainOutputNeuronCount.ReadOnly = true;
            this.txtDomainOutputNeuronCount.Size = new System.Drawing.Size(56, 20);
            this.txtDomainOutputNeuronCount.TabIndex = 38;
            // 
            // txtDomainInputNeuronCount
            // 
            this.txtDomainInputNeuronCount.Location = new System.Drawing.Point(8, 144);
            this.txtDomainInputNeuronCount.Name = "txtDomainInputNeuronCount";
            this.txtDomainInputNeuronCount.ReadOnly = true;
            this.txtDomainInputNeuronCount.Size = new System.Drawing.Size(56, 20);
            this.txtDomainInputNeuronCount.TabIndex = 37;
            // 
            // cmbDomain
            // 
            this.cmbDomain.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDomain.Location = new System.Drawing.Point(8, 16);
            this.cmbDomain.Name = "cmbDomain";
            this.cmbDomain.Size = new System.Drawing.Size(192, 21);
            this.cmbDomain.TabIndex = 36;
            this.cmbDomain.SelectedIndexChanged += new System.EventHandler(this.cmbDomain_SelectedIndexChanged);
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.btnSearchReset);
            this.groupBox6.Controls.Add(this.btnSearchStop);
            this.groupBox6.Controls.Add(this.btnSearchStart);
            this.groupBox6.Location = new System.Drawing.Point(232, 0);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(224, 56);
            this.groupBox6.TabIndex = 17;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Search Control";
            // 
            // btnSearchReset
            // 
            this.btnSearchReset.Location = new System.Drawing.Point(152, 16);
            this.btnSearchReset.Name = "btnSearchReset";
            this.btnSearchReset.Size = new System.Drawing.Size(64, 32);
            this.btnSearchReset.TabIndex = 2;
            this.btnSearchReset.Text = "Reset";
            this.btnSearchReset.Click += new System.EventHandler(this.btnSearchReset_Click);
            // 
            // btnSearchStop
            // 
            this.btnSearchStop.Location = new System.Drawing.Point(80, 16);
            this.btnSearchStop.Name = "btnSearchStop";
            this.btnSearchStop.Size = new System.Drawing.Size(64, 32);
            this.btnSearchStop.TabIndex = 1;
            this.btnSearchStop.Text = "Stop / Pause";
            this.btnSearchStop.Click += new System.EventHandler(this.btnSearchStop_Click);
            // 
            // btnSearchStart
            // 
            this.btnSearchStart.Enabled = false;
            this.btnSearchStart.Location = new System.Drawing.Point(8, 16);
            this.btnSearchStart.Name = "btnSearchStart";
            this.btnSearchStart.Size = new System.Drawing.Size(64, 32);
            this.btnSearchStart.TabIndex = 0;
            this.btnSearchStart.Text = "Start / Continue";
            this.btnSearchStart.Click += new System.EventHandler(this.btnSearchStart_Click);
            // 
            // gbxFile
            // 
            this.gbxFile.Controls.Add(this.label37);
            this.gbxFile.Controls.Add(this.txtFileLogBaseName);
            this.gbxFile.Controls.Add(this.chkFileWriteLog);
            this.gbxFile.Controls.Add(this.label13);
            this.gbxFile.Controls.Add(this.txtFileBaseName);
            this.gbxFile.Controls.Add(this.chkFileSaveGenomeOnImprovement);
            this.gbxFile.Location = new System.Drawing.Point(0, 240);
            this.gbxFile.Name = "gbxFile";
            this.gbxFile.Size = new System.Drawing.Size(224, 268);
            this.gbxFile.TabIndex = 16;
            this.gbxFile.TabStop = false;
            this.gbxFile.Text = "File";
            // 
            // label37
            // 
            this.label37.Location = new System.Drawing.Point(128, 88);
            this.label37.Name = "label37";
            this.label37.Size = new System.Drawing.Size(88, 16);
            this.label37.TabIndex = 26;
            this.label37.Text = "Filename prefix";
            // 
            // txtFileLogBaseName
            // 
            this.txtFileLogBaseName.Location = new System.Drawing.Point(8, 88);
            this.txtFileLogBaseName.Name = "txtFileLogBaseName";
            this.txtFileLogBaseName.Size = new System.Drawing.Size(120, 20);
            this.txtFileLogBaseName.TabIndex = 25;
            this.txtFileLogBaseName.Text = "log";
            // 
            // chkFileWriteLog
            // 
            this.chkFileWriteLog.Location = new System.Drawing.Point(8, 64);
            this.chkFileWriteLog.Name = "chkFileWriteLog";
            this.chkFileWriteLog.Size = new System.Drawing.Size(192, 24);
            this.chkFileWriteLog.TabIndex = 24;
            this.chkFileWriteLog.Text = "Write Log File";
            // 
            // label13
            // 
            this.label13.Location = new System.Drawing.Point(128, 44);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(88, 16);
            this.label13.TabIndex = 23;
            this.label13.Text = "Filename prefix";
            // 
            // txtFileBaseName
            // 
            this.txtFileBaseName.Location = new System.Drawing.Point(8, 40);
            this.txtFileBaseName.Name = "txtFileBaseName";
            this.txtFileBaseName.Size = new System.Drawing.Size(120, 20);
            this.txtFileBaseName.TabIndex = 1;
            this.txtFileBaseName.Text = "Luigi";
            // 
            // chkFileSaveGenomeOnImprovement
            // 
            this.chkFileSaveGenomeOnImprovement.Checked = true;
            this.chkFileSaveGenomeOnImprovement.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFileSaveGenomeOnImprovement.Location = new System.Drawing.Point(8, 16);
            this.chkFileSaveGenomeOnImprovement.Name = "chkFileSaveGenomeOnImprovement";
            this.chkFileSaveGenomeOnImprovement.Size = new System.Drawing.Size(192, 24);
            this.chkFileSaveGenomeOnImprovement.TabIndex = 0;
            this.chkFileSaveGenomeOnImprovement.Text = "Save Genome On Improvement";
            // 
            // gbxCurrentStats
            // 
            this.gbxCurrentStats.Controls.Add(this.label38);
            this.gbxCurrentStats.Controls.Add(this.txtStatsMode);
            this.gbxCurrentStats.Controls.Add(this.txtStatsEvaluationsPerSec);
            this.gbxCurrentStats.Controls.Add(this.label20);
            this.gbxCurrentStats.Controls.Add(this.label19);
            this.gbxCurrentStats.Controls.Add(this.label18);
            this.gbxCurrentStats.Controls.Add(this.txtStatsMeanGenomeLength);
            this.gbxCurrentStats.Controls.Add(this.txtStatsBestGenomeLength);
            this.gbxCurrentStats.Controls.Add(this.txtStatsTotalEvaluations);
            this.gbxCurrentStats.Controls.Add(this.label27);
            this.gbxCurrentStats.Controls.Add(this.txtStatsEvaluatorStateMsg);
            this.gbxCurrentStats.Controls.Add(this.label7);
            this.gbxCurrentStats.Controls.Add(this.txtStatsCompatibilityThreshold);
            this.gbxCurrentStats.Controls.Add(this.label5);
            this.gbxCurrentStats.Controls.Add(this.txtStatsGeneration);
            this.gbxCurrentStats.Controls.Add(this.label4);
            this.gbxCurrentStats.Controls.Add(this.label3);
            this.gbxCurrentStats.Controls.Add(this.label2);
            this.gbxCurrentStats.Controls.Add(this.txtStatsSpeciesCount);
            this.gbxCurrentStats.Controls.Add(this.txtStatsMean);
            this.gbxCurrentStats.Controls.Add(this.txtStatsBest);
            this.gbxCurrentStats.Controls.Add(this.label6);
            this.gbxCurrentStats.Location = new System.Drawing.Point(232, 56);
            this.gbxCurrentStats.Name = "gbxCurrentStats";
            this.gbxCurrentStats.Size = new System.Drawing.Size(224, 452);
            this.gbxCurrentStats.TabIndex = 8;
            this.gbxCurrentStats.TabStop = false;
            this.gbxCurrentStats.Text = "Current Stats";
            // 
            // label38
            // 
            this.label38.Location = new System.Drawing.Point(104, 256);
            this.label38.Name = "label38";
            this.label38.Size = new System.Drawing.Size(112, 16);
            this.label38.TabIndex = 21;
            this.label38.Text = "Current Search Mode";
            // 
            // txtStatsMode
            // 
            this.txtStatsMode.Location = new System.Drawing.Point(8, 256);
            this.txtStatsMode.Name = "txtStatsMode";
            this.txtStatsMode.ReadOnly = true;
            this.txtStatsMode.Size = new System.Drawing.Size(96, 20);
            this.txtStatsMode.TabIndex = 20;
            // 
            // txtStatsEvaluationsPerSec
            // 
            this.txtStatsEvaluationsPerSec.Location = new System.Drawing.Point(8, 184);
            this.txtStatsEvaluationsPerSec.Name = "txtStatsEvaluationsPerSec";
            this.txtStatsEvaluationsPerSec.ReadOnly = true;
            this.txtStatsEvaluationsPerSec.Size = new System.Drawing.Size(80, 20);
            this.txtStatsEvaluationsPerSec.TabIndex = 18;
            // 
            // label20
            // 
            this.label20.Location = new System.Drawing.Point(88, 184);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(104, 16);
            this.label20.TabIndex = 19;
            this.label20.Text = "Evaluations / Sec";
            // 
            // label19
            // 
            this.label19.Location = new System.Drawing.Point(104, 232);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(112, 16);
            this.label19.TabIndex = 17;
            this.label19.Text = "Avg. Genome Length";
            // 
            // label18
            // 
            this.label18.Location = new System.Drawing.Point(104, 208);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(112, 16);
            this.label18.TabIndex = 16;
            this.label18.Text = "Best Genome Length";
            // 
            // txtStatsMeanGenomeLength
            // 
            this.txtStatsMeanGenomeLength.Location = new System.Drawing.Point(8, 232);
            this.txtStatsMeanGenomeLength.Name = "txtStatsMeanGenomeLength";
            this.txtStatsMeanGenomeLength.ReadOnly = true;
            this.txtStatsMeanGenomeLength.Size = new System.Drawing.Size(96, 20);
            this.txtStatsMeanGenomeLength.TabIndex = 15;
            // 
            // txtStatsBestGenomeLength
            // 
            this.txtStatsBestGenomeLength.Location = new System.Drawing.Point(8, 208);
            this.txtStatsBestGenomeLength.Name = "txtStatsBestGenomeLength";
            this.txtStatsBestGenomeLength.ReadOnly = true;
            this.txtStatsBestGenomeLength.Size = new System.Drawing.Size(96, 20);
            this.txtStatsBestGenomeLength.TabIndex = 14;
            // 
            // txtStatsTotalEvaluations
            // 
            this.txtStatsTotalEvaluations.Location = new System.Drawing.Point(8, 160);
            this.txtStatsTotalEvaluations.Name = "txtStatsTotalEvaluations";
            this.txtStatsTotalEvaluations.ReadOnly = true;
            this.txtStatsTotalEvaluations.Size = new System.Drawing.Size(80, 20);
            this.txtStatsTotalEvaluations.TabIndex = 12;
            // 
            // label27
            // 
            this.label27.Location = new System.Drawing.Point(88, 163);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(104, 16);
            this.label27.TabIndex = 13;
            this.label27.Text = "Total Evaluations";
            // 
            // txtStatsEvaluatorStateMsg
            // 
            this.txtStatsEvaluatorStateMsg.Location = new System.Drawing.Point(8, 136);
            this.txtStatsEvaluatorStateMsg.Name = "txtStatsEvaluatorStateMsg";
            this.txtStatsEvaluatorStateMsg.ReadOnly = true;
            this.txtStatsEvaluatorStateMsg.Size = new System.Drawing.Size(80, 20);
            this.txtStatsEvaluatorStateMsg.TabIndex = 10;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(88, 136);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(120, 16);
            this.label7.TabIndex = 11;
            this.label7.Text = "Evaluator State Msg";
            // 
            // txtStatsCompatibilityThreshold
            // 
            this.txtStatsCompatibilityThreshold.Location = new System.Drawing.Point(8, 112);
            this.txtStatsCompatibilityThreshold.Name = "txtStatsCompatibilityThreshold";
            this.txtStatsCompatibilityThreshold.ReadOnly = true;
            this.txtStatsCompatibilityThreshold.Size = new System.Drawing.Size(80, 20);
            this.txtStatsCompatibilityThreshold.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(88, 16);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 16);
            this.label5.TabIndex = 7;
            this.label5.Text = "Generation";
            // 
            // txtStatsGeneration
            // 
            this.txtStatsGeneration.Location = new System.Drawing.Point(8, 16);
            this.txtStatsGeneration.Name = "txtStatsGeneration";
            this.txtStatsGeneration.ReadOnly = true;
            this.txtStatsGeneration.Size = new System.Drawing.Size(80, 20);
            this.txtStatsGeneration.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(88, 88);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 16);
            this.label4.TabIndex = 5;
            this.label4.Text = "# of Species ";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(88, 64);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 16);
            this.label3.TabIndex = 4;
            this.label3.Text = "Mean";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(88, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(40, 16);
            this.label2.TabIndex = 3;
            this.label2.Text = "Best";
            // 
            // txtStatsSpeciesCount
            // 
            this.txtStatsSpeciesCount.Location = new System.Drawing.Point(8, 88);
            this.txtStatsSpeciesCount.Name = "txtStatsSpeciesCount";
            this.txtStatsSpeciesCount.ReadOnly = true;
            this.txtStatsSpeciesCount.Size = new System.Drawing.Size(80, 20);
            this.txtStatsSpeciesCount.TabIndex = 2;
            // 
            // txtStatsMean
            // 
            this.txtStatsMean.Location = new System.Drawing.Point(8, 64);
            this.txtStatsMean.Name = "txtStatsMean";
            this.txtStatsMean.ReadOnly = true;
            this.txtStatsMean.Size = new System.Drawing.Size(80, 20);
            this.txtStatsMean.TabIndex = 1;
            // 
            // txtStatsBest
            // 
            this.txtStatsBest.Location = new System.Drawing.Point(8, 40);
            this.txtStatsBest.Name = "txtStatsBest";
            this.txtStatsBest.ReadOnly = true;
            this.txtStatsBest.Size = new System.Drawing.Size(80, 20);
            this.txtStatsBest.TabIndex = 0;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(88, 112);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(128, 16);
            this.label6.TabIndex = 9;
            this.label6.Text = "Compatibility Threshold";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.gbxSearchParameters);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(459, 514);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Page 2";
            // 
            // gbxSearchParameters
            // 
            this.gbxSearchParameters.Controls.Add(this.groupBox7);
            this.gbxSearchParameters.Controls.Add(this.txtParamPopulationSize);
            this.gbxSearchParameters.Controls.Add(this.label17);
            this.gbxSearchParameters.Controls.Add(this.txtParamConnectionWeightMutationSigma);
            this.gbxSearchParameters.Controls.Add(this.label16);
            this.gbxSearchParameters.Controls.Add(this.txtParamConnectionWeightRange);
            this.gbxSearchParameters.Controls.Add(this.groupBox4);
            this.gbxSearchParameters.Controls.Add(this.groupBox2);
            this.gbxSearchParameters.Controls.Add(this.groupBox3);
            this.gbxSearchParameters.Controls.Add(this.groupBox1);
            this.gbxSearchParameters.Controls.Add(this.label28);
            this.gbxSearchParameters.Location = new System.Drawing.Point(0, 0);
            this.gbxSearchParameters.Name = "gbxSearchParameters";
            this.gbxSearchParameters.Size = new System.Drawing.Size(456, 508);
            this.gbxSearchParameters.TabIndex = 15;
            this.gbxSearchParameters.TabStop = false;
            this.gbxSearchParameters.Text = "Search Parameters";
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.label41);
            this.groupBox7.Controls.Add(this.txtParamPruningEndComplexityStagnationThreshold);
            this.groupBox7.Controls.Add(this.label40);
            this.groupBox7.Controls.Add(this.txtParamPruningBeginFitnessStagnationThreshold);
            this.groupBox7.Controls.Add(this.label39);
            this.groupBox7.Controls.Add(this.txtParamPruningBeginComplexityThreshold);
            this.groupBox7.Controls.Add(this.chkParamEnableConnectionWeightFixing);
            this.groupBox7.Controls.Add(this.chkParamPruningModeEnabled);
            this.groupBox7.Location = new System.Drawing.Point(8, 168);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(216, 144);
            this.groupBox7.TabIndex = 54;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Pruning Phase Parameters";
            // 
            // label41
            // 
            this.label41.Location = new System.Drawing.Point(56, 109);
            this.label41.Name = "label41";
            this.label41.Size = new System.Drawing.Size(152, 24);
            this.label41.TabIndex = 55;
            this.label41.Text = "End Complexity Stagnation Threshold";
            // 
            // txtParamPruningEndComplexityStagnationThreshold
            // 
            this.txtParamPruningEndComplexityStagnationThreshold.Location = new System.Drawing.Point(8, 111);
            this.txtParamPruningEndComplexityStagnationThreshold.Name = "txtParamPruningEndComplexityStagnationThreshold";
            this.txtParamPruningEndComplexityStagnationThreshold.Size = new System.Drawing.Size(48, 20);
            this.txtParamPruningEndComplexityStagnationThreshold.TabIndex = 54;
            // 
            // label40
            // 
            this.label40.Location = new System.Drawing.Point(56, 78);
            this.label40.Name = "label40";
            this.label40.Size = new System.Drawing.Size(152, 31);
            this.label40.TabIndex = 34;
            this.label40.Text = "Begin Fitness Stagnation Threshold (gens)";
            // 
            // txtParamPruningBeginFitnessStagnationThreshold
            // 
            this.txtParamPruningBeginFitnessStagnationThreshold.Location = new System.Drawing.Point(8, 80);
            this.txtParamPruningBeginFitnessStagnationThreshold.Name = "txtParamPruningBeginFitnessStagnationThreshold";
            this.txtParamPruningBeginFitnessStagnationThreshold.Size = new System.Drawing.Size(48, 20);
            this.txtParamPruningBeginFitnessStagnationThreshold.TabIndex = 33;
            // 
            // label39
            // 
            this.label39.Location = new System.Drawing.Point(56, 58);
            this.label39.Name = "label39";
            this.label39.Size = new System.Drawing.Size(152, 16);
            this.label39.TabIndex = 32;
            this.label39.Text = "Begin Complexity Threshold";
            // 
            // txtParamPruningBeginComplexityThreshold
            // 
            this.txtParamPruningBeginComplexityThreshold.Location = new System.Drawing.Point(8, 56);
            this.txtParamPruningBeginComplexityThreshold.Name = "txtParamPruningBeginComplexityThreshold";
            this.txtParamPruningBeginComplexityThreshold.Size = new System.Drawing.Size(48, 20);
            this.txtParamPruningBeginComplexityThreshold.TabIndex = 31;
            // 
            // chkParamEnableConnectionWeightFixing
            // 
            this.chkParamEnableConnectionWeightFixing.Location = new System.Drawing.Point(8, 32);
            this.chkParamEnableConnectionWeightFixing.Name = "chkParamEnableConnectionWeightFixing";
            this.chkParamEnableConnectionWeightFixing.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.chkParamEnableConnectionWeightFixing.Size = new System.Drawing.Size(136, 16);
            this.chkParamEnableConnectionWeightFixing.TabIndex = 53;
            this.chkParamEnableConnectionWeightFixing.Text = "Enable Weight Fixing";
            // 
            // chkParamPruningModeEnabled
            // 
            this.chkParamPruningModeEnabled.Checked = true;
            this.chkParamPruningModeEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkParamPruningModeEnabled.Location = new System.Drawing.Point(8, 16);
            this.chkParamPruningModeEnabled.Name = "chkParamPruningModeEnabled";
            this.chkParamPruningModeEnabled.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.chkParamPruningModeEnabled.Size = new System.Drawing.Size(104, 16);
            this.chkParamPruningModeEnabled.TabIndex = 52;
            this.chkParamPruningModeEnabled.Text = "Enable Pruning";
            this.chkParamPruningModeEnabled.CheckedChanged += new System.EventHandler(this.chkParamPruningModeEnabled_CheckedChanged);
            // 
            // txtParamPopulationSize
            // 
            this.txtParamPopulationSize.Location = new System.Drawing.Point(16, 16);
            this.txtParamPopulationSize.Name = "txtParamPopulationSize";
            this.txtParamPopulationSize.Size = new System.Drawing.Size(48, 20);
            this.txtParamPopulationSize.TabIndex = 42;
            // 
            // label17
            // 
            this.label17.Enabled = false;
            this.label17.Location = new System.Drawing.Point(64, 344);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(160, 16);
            this.label17.TabIndex = 51;
            this.label17.Text = "Conn. Weight Mutation Sigma";
            // 
            // txtParamConnectionWeightMutationSigma
            // 
            this.txtParamConnectionWeightMutationSigma.Enabled = false;
            this.txtParamConnectionWeightMutationSigma.Location = new System.Drawing.Point(16, 344);
            this.txtParamConnectionWeightMutationSigma.Name = "txtParamConnectionWeightMutationSigma";
            this.txtParamConnectionWeightMutationSigma.Size = new System.Drawing.Size(48, 20);
            this.txtParamConnectionWeightMutationSigma.TabIndex = 50;
            // 
            // label16
            // 
            this.label16.Location = new System.Drawing.Point(64, 320);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(136, 16);
            this.label16.TabIndex = 49;
            this.label16.Text = "Connection Weight Range";
            // 
            // txtParamConnectionWeightRange
            // 
            this.txtParamConnectionWeightRange.Location = new System.Drawing.Point(16, 320);
            this.txtParamConnectionWeightRange.Name = "txtParamConnectionWeightRange";
            this.txtParamConnectionWeightRange.Size = new System.Drawing.Size(48, 20);
            this.txtParamConnectionWeightRange.TabIndex = 48;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label29);
            this.groupBox4.Controls.Add(this.txtParamSpeciesDropoffAge);
            this.groupBox4.Controls.Add(this.label30);
            this.groupBox4.Controls.Add(this.txtParamTargetSpeciesCountMax);
            this.groupBox4.Controls.Add(this.label31);
            this.groupBox4.Controls.Add(this.txtParamTargetSpeciesCountMin);
            this.groupBox4.Controls.Add(this.label32);
            this.groupBox4.Controls.Add(this.txtParamSelectionProportion);
            this.groupBox4.Controls.Add(this.label33);
            this.groupBox4.Controls.Add(this.txtParamElitismProportion);
            this.groupBox4.Location = new System.Drawing.Point(8, 40);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(216, 128);
            this.groupBox4.TabIndex = 47;
            this.groupBox4.TabStop = false;
            // 
            // label29
            // 
            this.label29.Location = new System.Drawing.Point(56, 104);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(128, 16);
            this.label29.TabIndex = 30;
            this.label29.Text = "Species Dropoff Age";
            // 
            // txtParamSpeciesDropoffAge
            // 
            this.txtParamSpeciesDropoffAge.Location = new System.Drawing.Point(8, 104);
            this.txtParamSpeciesDropoffAge.Name = "txtParamSpeciesDropoffAge";
            this.txtParamSpeciesDropoffAge.Size = new System.Drawing.Size(48, 20);
            this.txtParamSpeciesDropoffAge.TabIndex = 29;
            // 
            // label30
            // 
            this.label30.Location = new System.Drawing.Point(56, 80);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(128, 16);
            this.label30.TabIndex = 28;
            this.label30.Text = "Max Species Threshold";
            // 
            // txtParamTargetSpeciesCountMax
            // 
            this.txtParamTargetSpeciesCountMax.Location = new System.Drawing.Point(8, 80);
            this.txtParamTargetSpeciesCountMax.Name = "txtParamTargetSpeciesCountMax";
            this.txtParamTargetSpeciesCountMax.Size = new System.Drawing.Size(48, 20);
            this.txtParamTargetSpeciesCountMax.TabIndex = 27;
            // 
            // label31
            // 
            this.label31.Location = new System.Drawing.Point(56, 56);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(128, 16);
            this.label31.TabIndex = 26;
            this.label31.Text = "Min Species Threshold";
            // 
            // txtParamTargetSpeciesCountMin
            // 
            this.txtParamTargetSpeciesCountMin.Location = new System.Drawing.Point(8, 56);
            this.txtParamTargetSpeciesCountMin.Name = "txtParamTargetSpeciesCountMin";
            this.txtParamTargetSpeciesCountMin.Size = new System.Drawing.Size(48, 20);
            this.txtParamTargetSpeciesCountMin.TabIndex = 25;
            // 
            // label32
            // 
            this.label32.Location = new System.Drawing.Point(56, 32);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(120, 16);
            this.label32.TabIndex = 24;
            this.label32.Text = "Selection Proportion";
            // 
            // txtParamSelectionProportion
            // 
            this.txtParamSelectionProportion.Location = new System.Drawing.Point(8, 32);
            this.txtParamSelectionProportion.Name = "txtParamSelectionProportion";
            this.txtParamSelectionProportion.Size = new System.Drawing.Size(48, 20);
            this.txtParamSelectionProportion.TabIndex = 23;
            // 
            // label33
            // 
            this.label33.Location = new System.Drawing.Point(56, 8);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(96, 16);
            this.label33.TabIndex = 22;
            this.label33.Text = "Elitism Proportion";
            // 
            // txtParamElitismProportion
            // 
            this.txtParamElitismProportion.Location = new System.Drawing.Point(8, 8);
            this.txtParamElitismProportion.Name = "txtParamElitismProportion";
            this.txtParamElitismProportion.Size = new System.Drawing.Size(48, 20);
            this.txtParamElitismProportion.TabIndex = 21;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label43);
            this.groupBox2.Controls.Add(this.txtParamMutateDeleteNeuron);
            this.groupBox2.Controls.Add(this.label42);
            this.groupBox2.Controls.Add(this.txtParamMutateDeleteConnection);
            this.groupBox2.Controls.Add(this.label34);
            this.groupBox2.Controls.Add(this.txtParamMutateConnectionWeights);
            this.groupBox2.Controls.Add(this.label35);
            this.groupBox2.Controls.Add(this.txtParamMutateAddNode);
            this.groupBox2.Controls.Add(this.label36);
            this.groupBox2.Controls.Add(this.txtParamMutateAddConnection);
            this.groupBox2.Location = new System.Drawing.Point(232, 88);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(216, 128);
            this.groupBox2.TabIndex = 46;
            this.groupBox2.TabStop = false;
            // 
            // label43
            // 
            this.label43.Location = new System.Drawing.Point(56, 80);
            this.label43.Name = "label43";
            this.label43.Size = new System.Drawing.Size(144, 16);
            this.label43.TabIndex = 29;
            this.label43.Text = "p Mutate Delete Neuron";
            // 
            // txtParamMutateDeleteNeuron
            // 
            this.txtParamMutateDeleteNeuron.Location = new System.Drawing.Point(8, 80);
            this.txtParamMutateDeleteNeuron.Name = "txtParamMutateDeleteNeuron";
            this.txtParamMutateDeleteNeuron.Size = new System.Drawing.Size(48, 20);
            this.txtParamMutateDeleteNeuron.TabIndex = 28;
            // 
            // label42
            // 
            this.label42.Location = new System.Drawing.Point(56, 104);
            this.label42.Name = "label42";
            this.label42.Size = new System.Drawing.Size(152, 16);
            this.label42.TabIndex = 27;
            this.label42.Text = "p Mutate Delete Connection";
            // 
            // txtParamMutateDeleteConnection
            // 
            this.txtParamMutateDeleteConnection.Location = new System.Drawing.Point(8, 104);
            this.txtParamMutateDeleteConnection.Name = "txtParamMutateDeleteConnection";
            this.txtParamMutateDeleteConnection.Size = new System.Drawing.Size(48, 20);
            this.txtParamMutateDeleteConnection.TabIndex = 26;
            // 
            // label34
            // 
            this.label34.Location = new System.Drawing.Point(56, 56);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(136, 16);
            this.label34.TabIndex = 25;
            this.label34.Text = "p Mutate Add Connection";
            // 
            // txtParamMutateConnectionWeights
            // 
            this.txtParamMutateConnectionWeights.Location = new System.Drawing.Point(8, 8);
            this.txtParamMutateConnectionWeights.Name = "txtParamMutateConnectionWeights";
            this.txtParamMutateConnectionWeights.Size = new System.Drawing.Size(48, 20);
            this.txtParamMutateConnectionWeights.TabIndex = 24;
            // 
            // label35
            // 
            this.label35.Location = new System.Drawing.Point(56, 32);
            this.label35.Name = "label35";
            this.label35.Size = new System.Drawing.Size(136, 16);
            this.label35.TabIndex = 23;
            this.label35.Text = "p Mutate Add Neuron";
            // 
            // txtParamMutateAddNode
            // 
            this.txtParamMutateAddNode.Location = new System.Drawing.Point(8, 32);
            this.txtParamMutateAddNode.Name = "txtParamMutateAddNode";
            this.txtParamMutateAddNode.Size = new System.Drawing.Size(48, 20);
            this.txtParamMutateAddNode.TabIndex = 22;
            // 
            // label36
            // 
            this.label36.Location = new System.Drawing.Point(56, 8);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(152, 16);
            this.label36.TabIndex = 21;
            this.label36.Text = "p Mutate Connection Weights";
            // 
            // txtParamMutateAddConnection
            // 
            this.txtParamMutateAddConnection.Location = new System.Drawing.Point(8, 56);
            this.txtParamMutateAddConnection.Name = "txtParamMutateAddConnection";
            this.txtParamMutateAddConnection.Size = new System.Drawing.Size(48, 20);
            this.txtParamMutateAddConnection.TabIndex = 20;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label12);
            this.groupBox3.Controls.Add(this.txtParamCompatDisjointCoeff);
            this.groupBox3.Controls.Add(this.label22);
            this.groupBox3.Controls.Add(this.txtParamCompatExcessCoeff);
            this.groupBox3.Controls.Add(this.label23);
            this.groupBox3.Controls.Add(this.txtParamCompatWeightDeltaCoeff);
            this.groupBox3.Controls.Add(this.label24);
            this.groupBox3.Controls.Add(this.txtParamCompatThreshold);
            this.groupBox3.Location = new System.Drawing.Point(232, 216);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(216, 112);
            this.groupBox3.TabIndex = 45;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Speciation Parametes";
            // 
            // label12
            // 
            this.label12.Location = new System.Drawing.Point(56, 40);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(136, 16);
            this.label12.TabIndex = 35;
            this.label12.Text = "Compat. Disjoint Coeff.";
            // 
            // txtParamCompatDisjointCoeff
            // 
            this.txtParamCompatDisjointCoeff.Location = new System.Drawing.Point(8, 40);
            this.txtParamCompatDisjointCoeff.Name = "txtParamCompatDisjointCoeff";
            this.txtParamCompatDisjointCoeff.Size = new System.Drawing.Size(48, 20);
            this.txtParamCompatDisjointCoeff.TabIndex = 34;
            // 
            // label22
            // 
            this.label22.Location = new System.Drawing.Point(56, 64);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(120, 16);
            this.label22.TabIndex = 33;
            this.label22.Text = "Compat. Excess Coeff.";
            // 
            // txtParamCompatExcessCoeff
            // 
            this.txtParamCompatExcessCoeff.Location = new System.Drawing.Point(8, 64);
            this.txtParamCompatExcessCoeff.Name = "txtParamCompatExcessCoeff";
            this.txtParamCompatExcessCoeff.Size = new System.Drawing.Size(48, 20);
            this.txtParamCompatExcessCoeff.TabIndex = 32;
            // 
            // label23
            // 
            this.label23.Location = new System.Drawing.Point(56, 88);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(152, 16);
            this.label23.TabIndex = 31;
            this.label23.Text = "Compat. Weight Delta Coeff.";
            // 
            // txtParamCompatWeightDeltaCoeff
            // 
            this.txtParamCompatWeightDeltaCoeff.Location = new System.Drawing.Point(8, 88);
            this.txtParamCompatWeightDeltaCoeff.Name = "txtParamCompatWeightDeltaCoeff";
            this.txtParamCompatWeightDeltaCoeff.Size = new System.Drawing.Size(48, 20);
            this.txtParamCompatWeightDeltaCoeff.TabIndex = 30;
            // 
            // label24
            // 
            this.label24.Location = new System.Drawing.Point(56, 16);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(152, 16);
            this.label24.TabIndex = 29;
            this.label24.Text = "Compat. Threshold Start Val";
            // 
            // txtParamCompatThreshold
            // 
            this.txtParamCompatThreshold.Location = new System.Drawing.Point(8, 16);
            this.txtParamCompatThreshold.Name = "txtParamCompatThreshold";
            this.txtParamCompatThreshold.Size = new System.Drawing.Size(48, 20);
            this.txtParamCompatThreshold.TabIndex = 28;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.txtParamInterspeciesMating);
            this.groupBox1.Controls.Add(this.label25);
            this.groupBox1.Controls.Add(this.txtParamOffspringCrossover);
            this.groupBox1.Controls.Add(this.label26);
            this.groupBox1.Controls.Add(this.txtParamOffspringMutation);
            this.groupBox1.Location = new System.Drawing.Point(232, 8);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(216, 80);
            this.groupBox1.TabIndex = 44;
            this.groupBox1.TabStop = false;
            // 
            // label15
            // 
            this.label15.BackColor = System.Drawing.Color.Black;
            this.label15.Location = new System.Drawing.Point(8, 54);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(200, 1);
            this.label15.TabIndex = 24;
            // 
            // label14
            // 
            this.label14.Location = new System.Drawing.Point(56, 57);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(120, 16);
            this.label14.TabIndex = 23;
            this.label14.Text = "p Interspecies Mating";
            // 
            // txtParamInterspeciesMating
            // 
            this.txtParamInterspeciesMating.Location = new System.Drawing.Point(8, 56);
            this.txtParamInterspeciesMating.Name = "txtParamInterspeciesMating";
            this.txtParamInterspeciesMating.Size = new System.Drawing.Size(48, 20);
            this.txtParamInterspeciesMating.TabIndex = 22;
            // 
            // label25
            // 
            this.label25.Location = new System.Drawing.Point(56, 32);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(120, 16);
            this.label25.TabIndex = 21;
            this.label25.Text = "p Offspring Crossover";
            // 
            // txtParamOffspringCrossover
            // 
            this.txtParamOffspringCrossover.Location = new System.Drawing.Point(8, 32);
            this.txtParamOffspringCrossover.Name = "txtParamOffspringCrossover";
            this.txtParamOffspringCrossover.Size = new System.Drawing.Size(48, 20);
            this.txtParamOffspringCrossover.TabIndex = 20;
            // 
            // label26
            // 
            this.label26.Location = new System.Drawing.Point(56, 8);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(104, 16);
            this.label26.TabIndex = 19;
            this.label26.Text = "p Offspring Asexual";
            // 
            // txtParamOffspringMutation
            // 
            this.txtParamOffspringMutation.Location = new System.Drawing.Point(8, 8);
            this.txtParamOffspringMutation.Name = "txtParamOffspringMutation";
            this.txtParamOffspringMutation.Size = new System.Drawing.Size(48, 20);
            this.txtParamOffspringMutation.TabIndex = 18;
            // 
            // label28
            // 
            this.label28.Location = new System.Drawing.Point(64, 16);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(64, 16);
            this.label28.TabIndex = 43;
            this.label28.Text = "Pop Size";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupBox9);
            this.tabPage3.Controls.Add(this.groupBox10);
            this.tabPage3.Controls.Add(this.groupBox8);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(459, 524);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Page Luigi";
            // 
            // groupBox9
            // 
            this.groupBox9.Controls.Add(this.label65);
            this.groupBox9.Controls.Add(this.luigi_FitnessJumpPenalty);
            this.groupBox9.Controls.Add(this.label64);
            this.groupBox9.Controls.Add(this.luigi_FitnessBricks);
            this.groupBox9.Controls.Add(this.label63);
            this.groupBox9.Controls.Add(this.luigi_FitnessSuicidePenalty);
            this.groupBox9.Controls.Add(this.label62);
            this.groupBox9.Controls.Add(this.luigi_FitnessVictoryBonus);
            this.groupBox9.Controls.Add(this.luigi_UseGenerationStopThreshold);
            this.groupBox9.Controls.Add(this.luigi_FitnessUseStopThreshold);
            this.groupBox9.Controls.Add(this.label57);
            this.groupBox9.Controls.Add(this.luigi_GenerationStopThreshold);
            this.groupBox9.Controls.Add(this.label50);
            this.groupBox9.Controls.Add(this.luigi_FitnessStopThreshold);
            this.groupBox9.Controls.Add(this.label56);
            this.groupBox9.Controls.Add(this.luigi_FitnessPowerups);
            this.groupBox9.Controls.Add(this.label55);
            this.groupBox9.Controls.Add(this.luigi_FitnessKills);
            this.groupBox9.Controls.Add(this.label53);
            this.groupBox9.Controls.Add(this.luigi_FitnessLuigiSize);
            this.groupBox9.Controls.Add(this.label54);
            this.groupBox9.Controls.Add(this.luigi_FitnessCoin);
            this.groupBox9.Controls.Add(this.label52);
            this.groupBox9.Controls.Add(this.luigi_FitnessTime);
            this.groupBox9.Controls.Add(this.label51);
            this.groupBox9.Controls.Add(this.luigi_FitnessDistance);
            this.groupBox9.Controls.Add(this.label10);
            this.groupBox9.Controls.Add(this.luigi_NumberOfRuns);
            this.groupBox9.Location = new System.Drawing.Point(232, 92);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Size = new System.Drawing.Size(226, 432);
            this.groupBox9.TabIndex = 75;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "Evaluation Options";
            // 
            // label65
            // 
            this.label65.Location = new System.Drawing.Point(54, 358);
            this.label65.Name = "label65";
            this.label65.Size = new System.Drawing.Size(156, 16);
            this.label65.TabIndex = 102;
            this.label65.Text = "Fitness: Jump Penalty";
            // 
            // luigi_FitnessJumpPenalty
            // 
            this.luigi_FitnessJumpPenalty.Location = new System.Drawing.Point(6, 356);
            this.luigi_FitnessJumpPenalty.Name = "luigi_FitnessJumpPenalty";
            this.luigi_FitnessJumpPenalty.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessJumpPenalty.TabIndex = 101;
            // 
            // label64
            // 
            this.label64.Location = new System.Drawing.Point(54, 206);
            this.label64.Name = "label64";
            this.label64.Size = new System.Drawing.Size(156, 16);
            this.label64.TabIndex = 100;
            this.label64.Text = "Fitness: Bricks Coefficient";
            // 
            // luigi_FitnessBricks
            // 
            this.luigi_FitnessBricks.Location = new System.Drawing.Point(6, 204);
            this.luigi_FitnessBricks.Name = "luigi_FitnessBricks";
            this.luigi_FitnessBricks.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessBricks.TabIndex = 99;
            // 
            // label63
            // 
            this.label63.Location = new System.Drawing.Point(54, 333);
            this.label63.Name = "label63";
            this.label63.Size = new System.Drawing.Size(156, 16);
            this.label63.TabIndex = 98;
            this.label63.Text = "Fitness: Suicide Penalty (%)";
            // 
            // luigi_FitnessSuicidePenalty
            // 
            this.luigi_FitnessSuicidePenalty.Location = new System.Drawing.Point(6, 331);
            this.luigi_FitnessSuicidePenalty.Name = "luigi_FitnessSuicidePenalty";
            this.luigi_FitnessSuicidePenalty.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessSuicidePenalty.TabIndex = 97;
            // 
            // label62
            // 
            this.label62.Location = new System.Drawing.Point(54, 307);
            this.label62.Name = "label62";
            this.label62.Size = new System.Drawing.Size(156, 16);
            this.label62.TabIndex = 96;
            this.label62.Text = "Fitness: Victory Bonus (%)";
            // 
            // luigi_FitnessVictoryBonus
            // 
            this.luigi_FitnessVictoryBonus.Location = new System.Drawing.Point(6, 305);
            this.luigi_FitnessVictoryBonus.Name = "luigi_FitnessVictoryBonus";
            this.luigi_FitnessVictoryBonus.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessVictoryBonus.TabIndex = 95;
            // 
            // luigi_UseGenerationStopThreshold
            // 
            this.luigi_UseGenerationStopThreshold.Location = new System.Drawing.Point(57, 288);
            this.luigi_UseGenerationStopThreshold.Name = "luigi_UseGenerationStopThreshold";
            this.luigi_UseGenerationStopThreshold.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.luigi_UseGenerationStopThreshold.Size = new System.Drawing.Size(155, 16);
            this.luigi_UseGenerationStopThreshold.TabIndex = 94;
            this.luigi_UseGenerationStopThreshold.Text = "Use Generation Threshold";
            this.luigi_UseGenerationStopThreshold.CheckedChanged += new System.EventHandler(this.luigi_UseGenerationStopThreshold_CheckedChanged);
            // 
            // luigi_FitnessUseStopThreshold
            // 
            this.luigi_FitnessUseStopThreshold.Location = new System.Drawing.Point(57, 250);
            this.luigi_FitnessUseStopThreshold.Name = "luigi_FitnessUseStopThreshold";
            this.luigi_FitnessUseStopThreshold.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.luigi_FitnessUseStopThreshold.Size = new System.Drawing.Size(153, 16);
            this.luigi_FitnessUseStopThreshold.TabIndex = 75;
            this.luigi_FitnessUseStopThreshold.Text = "Use Fitness Threshold";
            this.luigi_FitnessUseStopThreshold.CheckedChanged += new System.EventHandler(this.luigi_FitnessUseStopThreshold_CheckedChanged);
            // 
            // label57
            // 
            this.label57.Location = new System.Drawing.Point(54, 269);
            this.label57.Name = "label57";
            this.label57.Size = new System.Drawing.Size(152, 16);
            this.label57.TabIndex = 93;
            this.label57.Text = "Generation Stop Threshold";
            // 
            // luigi_GenerationStopThreshold
            // 
            this.luigi_GenerationStopThreshold.Enabled = false;
            this.luigi_GenerationStopThreshold.Location = new System.Drawing.Point(6, 267);
            this.luigi_GenerationStopThreshold.Name = "luigi_GenerationStopThreshold";
            this.luigi_GenerationStopThreshold.Size = new System.Drawing.Size(48, 20);
            this.luigi_GenerationStopThreshold.TabIndex = 92;
            // 
            // label50
            // 
            this.label50.Location = new System.Drawing.Point(54, 232);
            this.label50.Name = "label50";
            this.label50.Size = new System.Drawing.Size(132, 16);
            this.label50.TabIndex = 90;
            this.label50.Text = "Fitness: Stop Threshold";
            // 
            // luigi_FitnessStopThreshold
            // 
            this.luigi_FitnessStopThreshold.Enabled = false;
            this.luigi_FitnessStopThreshold.Location = new System.Drawing.Point(6, 230);
            this.luigi_FitnessStopThreshold.Name = "luigi_FitnessStopThreshold";
            this.luigi_FitnessStopThreshold.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessStopThreshold.TabIndex = 89;
            // 
            // label56
            // 
            this.label56.Location = new System.Drawing.Point(54, 180);
            this.label56.Name = "label56";
            this.label56.Size = new System.Drawing.Size(156, 16);
            this.label56.TabIndex = 88;
            this.label56.Text = "Fitness: Powerups Coefficient";
            // 
            // luigi_FitnessPowerups
            // 
            this.luigi_FitnessPowerups.Location = new System.Drawing.Point(6, 178);
            this.luigi_FitnessPowerups.Name = "luigi_FitnessPowerups";
            this.luigi_FitnessPowerups.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessPowerups.TabIndex = 87;
            // 
            // label55
            // 
            this.label55.Location = new System.Drawing.Point(54, 154);
            this.label55.Name = "label55";
            this.label55.Size = new System.Drawing.Size(132, 16);
            this.label55.TabIndex = 86;
            this.label55.Text = "Fitness: Kills Coefficient";
            // 
            // luigi_FitnessKills
            // 
            this.luigi_FitnessKills.Location = new System.Drawing.Point(6, 152);
            this.luigi_FitnessKills.Name = "luigi_FitnessKills";
            this.luigi_FitnessKills.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessKills.TabIndex = 85;
            // 
            // label53
            // 
            this.label53.Location = new System.Drawing.Point(54, 128);
            this.label53.Name = "label53";
            this.label53.Size = new System.Drawing.Size(166, 16);
            this.label53.TabIndex = 84;
            this.label53.Text = "Fitness: Luigi Size Coefficient";
            // 
            // luigi_FitnessLuigiSize
            // 
            this.luigi_FitnessLuigiSize.Location = new System.Drawing.Point(6, 125);
            this.luigi_FitnessLuigiSize.Name = "luigi_FitnessLuigiSize";
            this.luigi_FitnessLuigiSize.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessLuigiSize.TabIndex = 83;
            // 
            // label54
            // 
            this.label54.Location = new System.Drawing.Point(54, 100);
            this.label54.Name = "label54";
            this.label54.Size = new System.Drawing.Size(152, 16);
            this.label54.TabIndex = 82;
            this.label54.Text = "Fitness: Coin coefficient";
            // 
            // luigi_FitnessCoin
            // 
            this.luigi_FitnessCoin.Location = new System.Drawing.Point(6, 98);
            this.luigi_FitnessCoin.Name = "luigi_FitnessCoin";
            this.luigi_FitnessCoin.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessCoin.TabIndex = 81;
            // 
            // label52
            // 
            this.label52.Location = new System.Drawing.Point(54, 74);
            this.label52.Name = "label52";
            this.label52.Size = new System.Drawing.Size(152, 16);
            this.label52.TabIndex = 80;
            this.label52.Text = "Fitness: Time Coefficient";
            // 
            // luigi_FitnessTime
            // 
            this.luigi_FitnessTime.Location = new System.Drawing.Point(6, 72);
            this.luigi_FitnessTime.Name = "luigi_FitnessTime";
            this.luigi_FitnessTime.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessTime.TabIndex = 79;
            // 
            // label51
            // 
            this.label51.Location = new System.Drawing.Point(54, 47);
            this.label51.Name = "label51";
            this.label51.Size = new System.Drawing.Size(152, 16);
            this.label51.TabIndex = 78;
            this.label51.Text = "Fitness: Distance coefficient";
            // 
            // luigi_FitnessDistance
            // 
            this.luigi_FitnessDistance.Location = new System.Drawing.Point(6, 45);
            this.luigi_FitnessDistance.Name = "luigi_FitnessDistance";
            this.luigi_FitnessDistance.Size = new System.Drawing.Size(48, 20);
            this.luigi_FitnessDistance.TabIndex = 77;
            // 
            // label10
            // 
            this.label10.Location = new System.Drawing.Point(54, 21);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(152, 16);
            this.label10.TabIndex = 76;
            this.label10.Text = "Number of Runs";
            // 
            // luigi_NumberOfRuns
            // 
            this.luigi_NumberOfRuns.Location = new System.Drawing.Point(6, 19);
            this.luigi_NumberOfRuns.Name = "luigi_NumberOfRuns";
            this.luigi_NumberOfRuns.Size = new System.Drawing.Size(48, 20);
            this.luigi_NumberOfRuns.TabIndex = 75;
            this.luigi_NumberOfRuns.TextChanged += new System.EventHandler(this.luigi_NumberOfRuns_TextChanged);
            // 
            // groupBox10
            // 
            this.groupBox10.Controls.Add(this.luigi_IncreaseLengthAmount);
            this.groupBox10.Controls.Add(this.luigi_IncreaseDifficultyAmount);
            this.groupBox10.Controls.Add(this.luigi_IncreaseLength);
            this.groupBox10.Controls.Add(this.luigi_IncreaseDifficulty);
            this.groupBox10.Controls.Add(this.luigi_VictorySound);
            this.groupBox10.Controls.Add(this.panel7);
            this.groupBox10.Controls.Add(this.luigi_UseInverseDistances);
            this.groupBox10.Controls.Add(this.label61);
            this.groupBox10.Controls.Add(this.label60);
            this.groupBox10.Controls.Add(this.label59);
            this.groupBox10.Controls.Add(this.label58);
            this.groupBox10.Controls.Add(this.label49);
            this.groupBox10.Controls.Add(this.panel6);
            this.groupBox10.Controls.Add(this.panel5);
            this.groupBox10.Controls.Add(this.label48);
            this.groupBox10.Controls.Add(this.luigi_LevelRandomizationSeedMax);
            this.groupBox10.Controls.Add(this.luigi_LevelRandomizationSeedMin);
            this.groupBox10.Controls.Add(this.luigi_UseRandomSeed);
            this.groupBox10.Controls.Add(this.luigi_EnableVisualization);
            this.groupBox10.Controls.Add(this.label46);
            this.groupBox10.Controls.Add(this.luigi_enableGameViewer);
            this.groupBox10.Controls.Add(this.luigi_LevelRandomizationSeed);
            this.groupBox10.Controls.Add(this.luigi_MaxFPS);
            this.groupBox10.Controls.Add(this.label45);
            this.groupBox10.Controls.Add(this.luigi_LevelLength);
            this.groupBox10.Controls.Add(this.panel4);
            this.groupBox10.Controls.Add(this.label44);
            this.groupBox10.Controls.Add(this.luigi_TimeLimit);
            this.groupBox10.Controls.Add(this.panel3);
            this.groupBox10.Controls.Add(this.luigi_StopSimAfterFirstWin);
            this.groupBox10.Controls.Add(this.label9);
            this.groupBox10.Controls.Add(this.luigi_LevelDifficulty);
            this.groupBox10.Location = new System.Drawing.Point(8, 3);
            this.groupBox10.Name = "groupBox10";
            this.groupBox10.Size = new System.Drawing.Size(216, 521);
            this.groupBox10.TabIndex = 57;
            this.groupBox10.TabStop = false;
            this.groupBox10.Text = "Simulation Options";
            // 
            // luigi_VictorySound
            // 
            this.luigi_VictorySound.AutoSize = true;
            this.luigi_VictorySound.Location = new System.Drawing.Point(12, 448);
            this.luigi_VictorySound.Name = "luigi_VictorySound";
            this.luigi_VictorySound.Size = new System.Drawing.Size(119, 17);
            this.luigi_VictorySound.TabIndex = 103;
            this.luigi_VictorySound.Text = "Play Sound On Win";
            this.luigi_VictorySound.UseVisualStyleBackColor = true;
            this.luigi_VictorySound.CheckedChanged += new System.EventHandler(this.luigi_VictorySound_CheckedChanged);
            // 
            // panel7
            // 
            this.panel7.Controls.Add(this.luigi_UseFullJumpScript);
            this.panel7.Controls.Add(this.luigi_UseNoJumpScript);
            this.panel7.Controls.Add(this.luigi_UseHybridJumpScript);
            this.panel7.Location = new System.Drawing.Point(9, 390);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(136, 54);
            this.panel7.TabIndex = 102;
            // 
            // luigi_UseFullJumpScript
            // 
            this.luigi_UseFullJumpScript.AutoSize = true;
            this.luigi_UseFullJumpScript.Checked = true;
            this.luigi_UseFullJumpScript.Location = new System.Drawing.Point(3, 33);
            this.luigi_UseFullJumpScript.Name = "luigi_UseFullJumpScript";
            this.luigi_UseFullJumpScript.Size = new System.Drawing.Size(99, 17);
            this.luigi_UseFullJumpScript.TabIndex = 58;
            this.luigi_UseFullJumpScript.TabStop = true;
            this.luigi_UseFullJumpScript.Text = "Full Jump Script";
            this.luigi_UseFullJumpScript.UseVisualStyleBackColor = true;
            // 
            // luigi_UseNoJumpScript
            // 
            this.luigi_UseNoJumpScript.AutoSize = true;
            this.luigi_UseNoJumpScript.Location = new System.Drawing.Point(3, 3);
            this.luigi_UseNoJumpScript.Name = "luigi_UseNoJumpScript";
            this.luigi_UseNoJumpScript.Size = new System.Drawing.Size(97, 17);
            this.luigi_UseNoJumpScript.TabIndex = 56;
            this.luigi_UseNoJumpScript.Text = "No Jump Script";
            this.luigi_UseNoJumpScript.UseVisualStyleBackColor = true;
            // 
            // luigi_UseHybridJumpScript
            // 
            this.luigi_UseHybridJumpScript.AutoSize = true;
            this.luigi_UseHybridJumpScript.Location = new System.Drawing.Point(3, 18);
            this.luigi_UseHybridJumpScript.Name = "luigi_UseHybridJumpScript";
            this.luigi_UseHybridJumpScript.Size = new System.Drawing.Size(113, 17);
            this.luigi_UseHybridJumpScript.TabIndex = 57;
            this.luigi_UseHybridJumpScript.Text = "Hybrid Jump Script";
            this.luigi_UseHybridJumpScript.UseVisualStyleBackColor = true;
            // 
            // luigi_UseInverseDistances
            // 
            this.luigi_UseInverseDistances.AutoSize = true;
            this.luigi_UseInverseDistances.Location = new System.Drawing.Point(11, 372);
            this.luigi_UseInverseDistances.Name = "luigi_UseInverseDistances";
            this.luigi_UseInverseDistances.Size = new System.Drawing.Size(133, 17);
            this.luigi_UseInverseDistances.TabIndex = 102;
            this.luigi_UseInverseDistances.Text = "Use Inverse Distances";
            this.luigi_UseInverseDistances.UseVisualStyleBackColor = true;
            // 
            // label61
            // 
            this.label61.Location = new System.Drawing.Point(118, 348);
            this.label61.Name = "label61";
            this.label61.Size = new System.Drawing.Size(78, 16);
            this.label61.TabIndex = 80;
            this.label61.Text = "Least Detailed";
            // 
            // label60
            // 
            this.label60.Location = new System.Drawing.Point(118, 333);
            this.label60.Name = "label60";
            this.label60.Size = new System.Drawing.Size(78, 16);
            this.label60.TabIndex = 79;
            this.label60.Text = "In the middle";
            // 
            // label59
            // 
            this.label59.Location = new System.Drawing.Point(118, 318);
            this.label59.Name = "label59";
            this.label59.Size = new System.Drawing.Size(78, 16);
            this.label59.TabIndex = 78;
            this.label59.Text = "Most Detailed";
            // 
            // label58
            // 
            this.label58.Location = new System.Drawing.Point(78, 295);
            this.label58.Name = "label58";
            this.label58.Size = new System.Drawing.Size(78, 16);
            this.label58.TabIndex = 77;
            this.label58.Text = "Enemy Z-level";
            // 
            // label49
            // 
            this.label49.Location = new System.Drawing.Point(6, 295);
            this.label49.Name = "label49";
            this.label49.Size = new System.Drawing.Size(78, 16);
            this.label49.TabIndex = 76;
            this.label49.Text = "Map Z-level";
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.luigi_EnemyZ2);
            this.panel6.Controls.Add(this.luigi_EnemyZ0);
            this.panel6.Controls.Add(this.luigi_EnemyZ1);
            this.panel6.Location = new System.Drawing.Point(78, 313);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(42, 54);
            this.panel6.TabIndex = 61;
            // 
            // luigi_EnemyZ2
            // 
            this.luigi_EnemyZ2.AutoSize = true;
            this.luigi_EnemyZ2.Location = new System.Drawing.Point(3, 33);
            this.luigi_EnemyZ2.Name = "luigi_EnemyZ2";
            this.luigi_EnemyZ2.Size = new System.Drawing.Size(31, 17);
            this.luigi_EnemyZ2.TabIndex = 58;
            this.luigi_EnemyZ2.Text = "2";
            this.luigi_EnemyZ2.UseVisualStyleBackColor = true;
            // 
            // luigi_EnemyZ0
            // 
            this.luigi_EnemyZ0.AutoSize = true;
            this.luigi_EnemyZ0.Location = new System.Drawing.Point(3, 3);
            this.luigi_EnemyZ0.Name = "luigi_EnemyZ0";
            this.luigi_EnemyZ0.Size = new System.Drawing.Size(31, 17);
            this.luigi_EnemyZ0.TabIndex = 56;
            this.luigi_EnemyZ0.Text = "0";
            this.luigi_EnemyZ0.UseVisualStyleBackColor = true;
            // 
            // luigi_EnemyZ1
            // 
            this.luigi_EnemyZ1.AutoSize = true;
            this.luigi_EnemyZ1.Checked = true;
            this.luigi_EnemyZ1.Location = new System.Drawing.Point(3, 18);
            this.luigi_EnemyZ1.Name = "luigi_EnemyZ1";
            this.luigi_EnemyZ1.Size = new System.Drawing.Size(31, 17);
            this.luigi_EnemyZ1.TabIndex = 57;
            this.luigi_EnemyZ1.TabStop = true;
            this.luigi_EnemyZ1.Text = "1";
            this.luigi_EnemyZ1.UseVisualStyleBackColor = true;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.luigi_MapZ2);
            this.panel5.Controls.Add(this.luigi_MapZ0);
            this.panel5.Controls.Add(this.luigi_MapZ1);
            this.panel5.Location = new System.Drawing.Point(9, 313);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(45, 54);
            this.panel5.TabIndex = 60;
            // 
            // luigi_MapZ2
            // 
            this.luigi_MapZ2.AutoSize = true;
            this.luigi_MapZ2.Location = new System.Drawing.Point(3, 33);
            this.luigi_MapZ2.Name = "luigi_MapZ2";
            this.luigi_MapZ2.Size = new System.Drawing.Size(31, 17);
            this.luigi_MapZ2.TabIndex = 58;
            this.luigi_MapZ2.Text = "2";
            this.luigi_MapZ2.UseVisualStyleBackColor = true;
            // 
            // luigi_MapZ0
            // 
            this.luigi_MapZ0.AutoSize = true;
            this.luigi_MapZ0.Location = new System.Drawing.Point(3, 3);
            this.luigi_MapZ0.Name = "luigi_MapZ0";
            this.luigi_MapZ0.Size = new System.Drawing.Size(31, 17);
            this.luigi_MapZ0.TabIndex = 56;
            this.luigi_MapZ0.Text = "0";
            this.luigi_MapZ0.UseVisualStyleBackColor = true;
            // 
            // luigi_MapZ1
            // 
            this.luigi_MapZ1.AutoSize = true;
            this.luigi_MapZ1.Checked = true;
            this.luigi_MapZ1.Location = new System.Drawing.Point(3, 18);
            this.luigi_MapZ1.Name = "luigi_MapZ1";
            this.luigi_MapZ1.Size = new System.Drawing.Size(31, 17);
            this.luigi_MapZ1.TabIndex = 57;
            this.luigi_MapZ1.TabStop = true;
            this.luigi_MapZ1.Text = "1";
            this.luigi_MapZ1.UseVisualStyleBackColor = true;
            // 
            // label48
            // 
            this.label48.Location = new System.Drawing.Point(113, 279);
            this.label48.Name = "label48";
            this.label48.Size = new System.Drawing.Size(21, 16);
            this.label48.TabIndex = 75;
            this.label48.Text = "to";
            // 
            // luigi_LevelRandomizationSeedMax
            // 
            this.luigi_LevelRandomizationSeedMax.Enabled = false;
            this.luigi_LevelRandomizationSeedMax.Location = new System.Drawing.Point(140, 276);
            this.luigi_LevelRandomizationSeedMax.Name = "luigi_LevelRandomizationSeedMax";
            this.luigi_LevelRandomizationSeedMax.Size = new System.Drawing.Size(48, 20);
            this.luigi_LevelRandomizationSeedMax.TabIndex = 71;
            // 
            // luigi_LevelRandomizationSeedMin
            // 
            this.luigi_LevelRandomizationSeedMin.Enabled = false;
            this.luigi_LevelRandomizationSeedMin.Location = new System.Drawing.Point(57, 276);
            this.luigi_LevelRandomizationSeedMin.Name = "luigi_LevelRandomizationSeedMin";
            this.luigi_LevelRandomizationSeedMin.Size = new System.Drawing.Size(48, 20);
            this.luigi_LevelRandomizationSeedMin.TabIndex = 70;
            // 
            // luigi_UseRandomSeed
            // 
            this.luigi_UseRandomSeed.Location = new System.Drawing.Point(57, 256);
            this.luigi_UseRandomSeed.Name = "luigi_UseRandomSeed";
            this.luigi_UseRandomSeed.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.luigi_UseRandomSeed.Size = new System.Drawing.Size(132, 16);
            this.luigi_UseRandomSeed.TabIndex = 69;
            this.luigi_UseRandomSeed.Text = "Use random seed";
            this.luigi_UseRandomSeed.CheckedChanged += new System.EventHandler(this.luigi_UseRandomSeed_CheckedChanged);
            // 
            // luigi_EnableVisualization
            // 
            this.luigi_EnableVisualization.Location = new System.Drawing.Point(6, 51);
            this.luigi_EnableVisualization.Name = "luigi_EnableVisualization";
            this.luigi_EnableVisualization.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.luigi_EnableVisualization.Size = new System.Drawing.Size(132, 16);
            this.luigi_EnableVisualization.TabIndex = 68;
            this.luigi_EnableVisualization.Text = "Enable Visualization";
            this.luigi_EnableVisualization.CheckedChanged += new System.EventHandler(this.luigi_EnableVisualization_CheckedChanged);
            // 
            // label46
            // 
            this.label46.Location = new System.Drawing.Point(54, 235);
            this.label46.Name = "label46";
            this.label46.Size = new System.Drawing.Size(152, 16);
            this.label46.TabIndex = 67;
            this.label46.Text = "Level Randomization Seed";
            // 
            // luigi_enableGameViewer
            // 
            this.luigi_enableGameViewer.Location = new System.Drawing.Point(6, 35);
            this.luigi_enableGameViewer.Name = "luigi_enableGameViewer";
            this.luigi_enableGameViewer.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.luigi_enableGameViewer.Size = new System.Drawing.Size(136, 16);
            this.luigi_enableGameViewer.TabIndex = 53;
            this.luigi_enableGameViewer.Text = "Enable Game Viewer";
            // 
            // luigi_LevelRandomizationSeed
            // 
            this.luigi_LevelRandomizationSeed.Location = new System.Drawing.Point(6, 233);
            this.luigi_LevelRandomizationSeed.Name = "luigi_LevelRandomizationSeed";
            this.luigi_LevelRandomizationSeed.Size = new System.Drawing.Size(48, 20);
            this.luigi_LevelRandomizationSeed.TabIndex = 66;
            // 
            // luigi_MaxFPS
            // 
            this.luigi_MaxFPS.Location = new System.Drawing.Point(6, 19);
            this.luigi_MaxFPS.Name = "luigi_MaxFPS";
            this.luigi_MaxFPS.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.luigi_MaxFPS.Size = new System.Drawing.Size(104, 16);
            this.luigi_MaxFPS.TabIndex = 52;
            this.luigi_MaxFPS.Text = "Maximize FPS";
            this.luigi_MaxFPS.CheckedChanged += new System.EventHandler(this.luigi_MaxFPS_CheckedChanged);
            // 
            // label45
            // 
            this.label45.Location = new System.Drawing.Point(54, 182);
            this.label45.Name = "label45";
            this.label45.Size = new System.Drawing.Size(152, 16);
            this.label45.TabIndex = 65;
            this.label45.Text = "Level Length (50-4096)";
            // 
            // luigi_LevelLength
            // 
            this.luigi_LevelLength.Location = new System.Drawing.Point(6, 180);
            this.luigi_LevelLength.Name = "luigi_LevelLength";
            this.luigi_LevelLength.Size = new System.Drawing.Size(48, 20);
            this.luigi_LevelLength.TabIndex = 64;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.luigi_Random);
            this.panel4.Controls.Add(this.luigi_Castle);
            this.panel4.Controls.Add(this.luigi_Overground);
            this.panel4.Controls.Add(this.luigi_Underground);
            this.panel4.Location = new System.Drawing.Point(93, 89);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(95, 64);
            this.panel4.TabIndex = 60;
            // 
            // luigi_Random
            // 
            this.luigi_Random.AutoSize = true;
            this.luigi_Random.Checked = true;
            this.luigi_Random.Location = new System.Drawing.Point(3, 48);
            this.luigi_Random.Name = "luigi_Random";
            this.luigi_Random.Size = new System.Drawing.Size(65, 17);
            this.luigi_Random.TabIndex = 59;
            this.luigi_Random.TabStop = true;
            this.luigi_Random.Text = "Random";
            this.luigi_Random.UseVisualStyleBackColor = true;
            // 
            // luigi_Castle
            // 
            this.luigi_Castle.AutoSize = true;
            this.luigi_Castle.Location = new System.Drawing.Point(3, 33);
            this.luigi_Castle.Name = "luigi_Castle";
            this.luigi_Castle.Size = new System.Drawing.Size(54, 17);
            this.luigi_Castle.TabIndex = 58;
            this.luigi_Castle.Text = "Castle";
            this.luigi_Castle.UseVisualStyleBackColor = true;
            // 
            // luigi_Overground
            // 
            this.luigi_Overground.AutoSize = true;
            this.luigi_Overground.Location = new System.Drawing.Point(3, 3);
            this.luigi_Overground.Name = "luigi_Overground";
            this.luigi_Overground.Size = new System.Drawing.Size(81, 17);
            this.luigi_Overground.TabIndex = 56;
            this.luigi_Overground.Text = "Overground";
            this.luigi_Overground.UseVisualStyleBackColor = true;
            // 
            // luigi_Underground
            // 
            this.luigi_Underground.AutoSize = true;
            this.luigi_Underground.Location = new System.Drawing.Point(3, 18);
            this.luigi_Underground.Name = "luigi_Underground";
            this.luigi_Underground.Size = new System.Drawing.Size(87, 17);
            this.luigi_Underground.TabIndex = 57;
            this.luigi_Underground.Text = "Underground";
            this.luigi_Underground.UseVisualStyleBackColor = true;
            // 
            // label44
            // 
            this.label44.Location = new System.Drawing.Point(54, 208);
            this.label44.Name = "label44";
            this.label44.Size = new System.Drawing.Size(152, 16);
            this.label44.TabIndex = 63;
            this.label44.Text = "Time Limit (1-maxint)";
            // 
            // luigi_TimeLimit
            // 
            this.luigi_TimeLimit.Location = new System.Drawing.Point(6, 206);
            this.luigi_TimeLimit.Name = "luigi_TimeLimit";
            this.luigi_TimeLimit.Size = new System.Drawing.Size(48, 20);
            this.luigi_TimeLimit.TabIndex = 62;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.luigi_LuigiFire);
            this.panel3.Controls.Add(this.luigi_LuigiSmall);
            this.panel3.Controls.Add(this.luigi_LuigiBig);
            this.panel3.Location = new System.Drawing.Point(6, 89);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(81, 54);
            this.panel3.TabIndex = 59;
            // 
            // luigi_LuigiFire
            // 
            this.luigi_LuigiFire.AutoSize = true;
            this.luigi_LuigiFire.Checked = true;
            this.luigi_LuigiFire.Location = new System.Drawing.Point(3, 33);
            this.luigi_LuigiFire.Name = "luigi_LuigiFire";
            this.luigi_LuigiFire.Size = new System.Drawing.Size(64, 17);
            this.luigi_LuigiFire.TabIndex = 58;
            this.luigi_LuigiFire.TabStop = true;
            this.luigi_LuigiFire.Text = "Luigi fire";
            this.luigi_LuigiFire.UseVisualStyleBackColor = true;
            // 
            // luigi_LuigiSmall
            // 
            this.luigi_LuigiSmall.AutoSize = true;
            this.luigi_LuigiSmall.Location = new System.Drawing.Point(3, 3);
            this.luigi_LuigiSmall.Name = "luigi_LuigiSmall";
            this.luigi_LuigiSmall.Size = new System.Drawing.Size(73, 17);
            this.luigi_LuigiSmall.TabIndex = 56;
            this.luigi_LuigiSmall.Text = "Luigi small";
            this.luigi_LuigiSmall.UseVisualStyleBackColor = true;
            // 
            // luigi_LuigiBig
            // 
            this.luigi_LuigiBig.AutoSize = true;
            this.luigi_LuigiBig.Location = new System.Drawing.Point(3, 18);
            this.luigi_LuigiBig.Name = "luigi_LuigiBig";
            this.luigi_LuigiBig.Size = new System.Drawing.Size(64, 17);
            this.luigi_LuigiBig.TabIndex = 57;
            this.luigi_LuigiBig.Text = "Luigi big";
            this.luigi_LuigiBig.UseVisualStyleBackColor = true;
            // 
            // luigi_StopSimAfterFirstWin
            // 
            this.luigi_StopSimAfterFirstWin.Location = new System.Drawing.Point(6, 67);
            this.luigi_StopSimAfterFirstWin.Name = "luigi_StopSimAfterFirstWin";
            this.luigi_StopSimAfterFirstWin.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.luigi_StopSimAfterFirstWin.Size = new System.Drawing.Size(144, 16);
            this.luigi_StopSimAfterFirstWin.TabIndex = 54;
            this.luigi_StopSimAfterFirstWin.Text = "Stop Sim after first win";
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(54, 156);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(152, 16);
            this.label9.TabIndex = 55;
            this.label9.Text = "Level Difficulty (0-30)";
            // 
            // luigi_LevelDifficulty
            // 
            this.luigi_LevelDifficulty.Location = new System.Drawing.Point(6, 154);
            this.luigi_LevelDifficulty.Name = "luigi_LevelDifficulty";
            this.luigi_LevelDifficulty.Size = new System.Drawing.Size(48, 20);
            this.luigi_LevelDifficulty.TabIndex = 54;
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.label47);
            this.groupBox8.Controls.Add(this.luigi_RandomPortMax);
            this.groupBox8.Controls.Add(this.luigi_RandomPortMin);
            this.groupBox8.Controls.Add(this.luigi_RandomPort);
            this.groupBox8.Controls.Add(this.label11);
            this.groupBox8.Controls.Add(this.luigi_PortNumber);
            this.groupBox8.Location = new System.Drawing.Point(232, 3);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(226, 83);
            this.groupBox8.TabIndex = 55;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "Command Line Options";
            // 
            // label47
            // 
            this.label47.Location = new System.Drawing.Point(111, 61);
            this.label47.Name = "label47";
            this.label47.Size = new System.Drawing.Size(21, 16);
            this.label47.TabIndex = 74;
            this.label47.Text = "to";
            // 
            // luigi_RandomPortMax
            // 
            this.luigi_RandomPortMax.Enabled = false;
            this.luigi_RandomPortMax.Location = new System.Drawing.Point(138, 58);
            this.luigi_RandomPortMax.Name = "luigi_RandomPortMax";
            this.luigi_RandomPortMax.Size = new System.Drawing.Size(48, 20);
            this.luigi_RandomPortMax.TabIndex = 73;
            // 
            // luigi_RandomPortMin
            // 
            this.luigi_RandomPortMin.Enabled = false;
            this.luigi_RandomPortMin.Location = new System.Drawing.Point(57, 58);
            this.luigi_RandomPortMin.Name = "luigi_RandomPortMin";
            this.luigi_RandomPortMin.Size = new System.Drawing.Size(48, 20);
            this.luigi_RandomPortMin.TabIndex = 72;
            // 
            // luigi_RandomPort
            // 
            this.luigi_RandomPort.Location = new System.Drawing.Point(57, 37);
            this.luigi_RandomPort.Name = "luigi_RandomPort";
            this.luigi_RandomPort.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.luigi_RandomPort.Size = new System.Drawing.Size(112, 16);
            this.luigi_RandomPort.TabIndex = 54;
            this.luigi_RandomPort.Text = "Random Port";
            this.luigi_RandomPort.CheckedChanged += new System.EventHandler(this.luigi_RandomPort_CheckedChanged);
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(54, 17);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(152, 16);
            this.label11.TabIndex = 32;
            this.label11.Text = "Port Number";
            // 
            // luigi_PortNumber
            // 
            this.luigi_PortNumber.Location = new System.Drawing.Point(6, 15);
            this.luigi_PortNumber.Name = "luigi_PortNumber";
            this.luigi_PortNumber.Size = new System.Drawing.Size(48, 20);
            this.luigi_PortNumber.TabIndex = 31;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.gbxLog);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 563);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(467, 180);
            this.panel2.TabIndex = 6;
            // 
            // gbxLog
            // 
            this.gbxLog.Controls.Add(this.txtLogWindow);
            this.gbxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbxLog.Location = new System.Drawing.Point(0, 0);
            this.gbxLog.Name = "gbxLog";
            this.gbxLog.Size = new System.Drawing.Size(467, 180);
            this.gbxLog.TabIndex = 5;
            this.gbxLog.TabStop = false;
            this.gbxLog.Text = "Log";
            // 
            // txtLogWindow
            // 
            this.txtLogWindow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLogWindow.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLogWindow.Location = new System.Drawing.Point(3, 16);
            this.txtLogWindow.Multiline = true;
            this.txtLogWindow.Name = "txtLogWindow";
            this.txtLogWindow.ReadOnly = true;
            this.txtLogWindow.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLogWindow.Size = new System.Drawing.Size(461, 161);
            this.txtLogWindow.TabIndex = 5;
            this.txtLogWindow.WordWrap = false;
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.mnuInitPop,
            this.mnuView,
            this.mnuVisualization,
            this.mnuAbout});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuFileSave});
            this.menuItem1.Text = "File";
            // 
            // mnuFileSave
            // 
            this.mnuFileSave.Index = 0;
            this.mnuFileSave.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuFileSavePopulation,
            this.mnuFileSaveBestAsNetwork,
            this.mnuFileSaveBestAsGenome});
            this.mnuFileSave.Text = "Save";
            // 
            // mnuFileSavePopulation
            // 
            this.mnuFileSavePopulation.Index = 0;
            this.mnuFileSavePopulation.Text = "Save Population";
            this.mnuFileSavePopulation.Click += new System.EventHandler(this.mnuFileSavePopulation_Click);
            // 
            // mnuFileSaveBestAsNetwork
            // 
            this.mnuFileSaveBestAsNetwork.Index = 1;
            this.mnuFileSaveBestAsNetwork.Text = "Save Best As Network";
            this.mnuFileSaveBestAsNetwork.Click += new System.EventHandler(this.mnuFileSaveBestAsNetwork_Click);
            // 
            // mnuFileSaveBestAsGenome
            // 
            this.mnuFileSaveBestAsGenome.Index = 2;
            this.mnuFileSaveBestAsGenome.Text = "Save Best As Genome";
            this.mnuFileSaveBestAsGenome.Click += new System.EventHandler(this.mnuFileSaveBestAsGenome_Click);
            // 
            // mnuInitPop
            // 
            this.mnuInitPop.Index = 1;
            this.mnuInitPop.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuInitPopLoad,
            this.mnuInitPopAutoGenerate});
            this.mnuInitPop.Text = "Initialize Population";
            // 
            // mnuInitPopLoad
            // 
            this.mnuInitPopLoad.Index = 0;
            this.mnuInitPopLoad.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuInitPopLoadPopulation,
            this.mnuInitPopLoadSeedGenome,
            this.mnuInitPopLoadSeedPopulation});
            this.mnuInitPopLoad.Text = "Load";
            // 
            // mnuInitPopLoadPopulation
            // 
            this.mnuInitPopLoadPopulation.Index = 0;
            this.mnuInitPopLoadPopulation.Text = "Load Population";
            this.mnuInitPopLoadPopulation.Click += new System.EventHandler(this.mnuInitPopLoadPopulation_Click);
            // 
            // mnuInitPopLoadSeedGenome
            // 
            this.mnuInitPopLoadSeedGenome.Index = 1;
            this.mnuInitPopLoadSeedGenome.Text = "Load Seed Genome";
            this.mnuInitPopLoadSeedGenome.Click += new System.EventHandler(this.mnuInitPopLoadSeedGenome_Click);
            // 
            // mnuInitPopLoadSeedPopulation
            // 
            this.mnuInitPopLoadSeedPopulation.Index = 2;
            this.mnuInitPopLoadSeedPopulation.Text = "Load Seed Population";
            this.mnuInitPopLoadSeedPopulation.Click += new System.EventHandler(this.mnuInitPopLoadSeedPopulation_Click);
            // 
            // mnuInitPopAutoGenerate
            // 
            this.mnuInitPopAutoGenerate.Index = 1;
            this.mnuInitPopAutoGenerate.Text = "Auto Generate";
            this.mnuInitPopAutoGenerate.Click += new System.EventHandler(this.mnuInitPopAutoGenerate_Click);
            // 
            // mnuView
            // 
            this.mnuView.Index = 2;
            this.mnuView.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuViewUpdateFrequency});
            this.mnuView.Text = "View";
            // 
            // mnuViewUpdateFrequency
            // 
            this.mnuViewUpdateFrequency.Index = 0;
            this.mnuViewUpdateFrequency.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuViewUpdateFrequency1Sec,
            this.mnuViewUpdateFrequency2Sec,
            this.mnuViewUpdateFrequency5Sec,
            this.mnuViewUpdateFrequency10Sec,
            this.mnuViewUpdateFrequency1Gen,
            this.mnuViewUpdateFrequency2Gen,
            this.mnuViewUpdateFrequency5Gen,
            this.mnuViewUpdateFrequency10Gen});
            this.mnuViewUpdateFrequency.Text = "Update Frequency";
            // 
            // mnuViewUpdateFrequency1Sec
            // 
            this.mnuViewUpdateFrequency1Sec.Checked = true;
            this.mnuViewUpdateFrequency1Sec.Index = 0;
            this.mnuViewUpdateFrequency1Sec.RadioCheck = true;
            this.mnuViewUpdateFrequency1Sec.Text = "1 Second";
            this.mnuViewUpdateFrequency1Sec.Click += new System.EventHandler(this.mnuViewUpdateFrequency1Sec_Click);
            // 
            // mnuViewUpdateFrequency2Sec
            // 
            this.mnuViewUpdateFrequency2Sec.Index = 1;
            this.mnuViewUpdateFrequency2Sec.RadioCheck = true;
            this.mnuViewUpdateFrequency2Sec.Text = "2 Seconds";
            this.mnuViewUpdateFrequency2Sec.Click += new System.EventHandler(this.mnuViewUpdateFrequency2Sec_Click);
            // 
            // mnuViewUpdateFrequency5Sec
            // 
            this.mnuViewUpdateFrequency5Sec.Index = 2;
            this.mnuViewUpdateFrequency5Sec.RadioCheck = true;
            this.mnuViewUpdateFrequency5Sec.Text = "5 Seconds";
            this.mnuViewUpdateFrequency5Sec.Click += new System.EventHandler(this.mnuViewUpdateFrequency5Sec_Click);
            // 
            // mnuViewUpdateFrequency10Sec
            // 
            this.mnuViewUpdateFrequency10Sec.Index = 3;
            this.mnuViewUpdateFrequency10Sec.RadioCheck = true;
            this.mnuViewUpdateFrequency10Sec.Text = "10 Seconds";
            this.mnuViewUpdateFrequency10Sec.Click += new System.EventHandler(this.mnuViewUpdateFrequency10Sec_Click);
            // 
            // mnuViewUpdateFrequency1Gen
            // 
            this.mnuViewUpdateFrequency1Gen.Index = 4;
            this.mnuViewUpdateFrequency1Gen.RadioCheck = true;
            this.mnuViewUpdateFrequency1Gen.Text = "1 Generation";
            this.mnuViewUpdateFrequency1Gen.Click += new System.EventHandler(this.mnuViewUpdateFrequency1Gen_Click);
            // 
            // mnuViewUpdateFrequency2Gen
            // 
            this.mnuViewUpdateFrequency2Gen.Index = 5;
            this.mnuViewUpdateFrequency2Gen.RadioCheck = true;
            this.mnuViewUpdateFrequency2Gen.Text = "2 Generations";
            this.mnuViewUpdateFrequency2Gen.Click += new System.EventHandler(this.mnuViewUpdateFrequency2Gen_Click);
            // 
            // mnuViewUpdateFrequency5Gen
            // 
            this.mnuViewUpdateFrequency5Gen.Index = 6;
            this.mnuViewUpdateFrequency5Gen.RadioCheck = true;
            this.mnuViewUpdateFrequency5Gen.Text = "5 Generations";
            this.mnuViewUpdateFrequency5Gen.Click += new System.EventHandler(this.mnuViewUpdateFrequency5Gen_Click);
            // 
            // mnuViewUpdateFrequency10Gen
            // 
            this.mnuViewUpdateFrequency10Gen.Index = 7;
            this.mnuViewUpdateFrequency10Gen.RadioCheck = true;
            this.mnuViewUpdateFrequency10Gen.Text = "10 Generations";
            this.mnuViewUpdateFrequency10Gen.Click += new System.EventHandler(this.mnuViewUpdateFrequency10Gen_Click);
            // 
            // mnuVisualization
            // 
            this.mnuVisualization.Index = 3;
            this.mnuVisualization.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuVisualizationProgressGraph,
            this.mnuVisualizationBest,
            this.mnuVisualizationSpecies,
            this.mnuVisualizationExperiment});
            this.mnuVisualization.Text = "Visualization";
            // 
            // mnuVisualizationProgressGraph
            // 
            this.mnuVisualizationProgressGraph.Index = 0;
            this.mnuVisualizationProgressGraph.Text = "Progress Graphs";
            this.mnuVisualizationProgressGraph.Click += new System.EventHandler(this.mnuVisualizationProgressGraph_Click);
            // 
            // mnuVisualizationBest
            // 
            this.mnuVisualizationBest.Index = 1;
            this.mnuVisualizationBest.Text = "Best Genome";
            this.mnuVisualizationBest.Click += new System.EventHandler(this.mnuVisualizationBest_Click);
            // 
            // mnuVisualizationSpecies
            // 
            this.mnuVisualizationSpecies.Index = 2;
            this.mnuVisualizationSpecies.Text = "Species";
            this.mnuVisualizationSpecies.Click += new System.EventHandler(this.mnuVisualizationSpecies_Click);
            // 
            // mnuVisualizationExperiment
            // 
            this.mnuVisualizationExperiment.Index = 3;
            this.mnuVisualizationExperiment.Text = "Experiment";
            this.mnuVisualizationExperiment.Click += new System.EventHandler(this.mnuVisualizationExperiment_Click);
            // 
            // mnuAbout
            // 
            this.mnuAbout.Index = 4;
            this.mnuAbout.Text = "About";
            this.mnuAbout.Click += new System.EventHandler(this.mnuAbout_Click);
            // 
            // splitter1
            // 
            this.splitter1.BackColor = System.Drawing.SystemColors.Control;
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 550);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(467, 13);
            this.splitter1.TabIndex = 5;
            this.splitter1.TabStop = false;
            // 
            // luigi_IncreaseDifficulty
            // 
            this.luigi_IncreaseDifficulty.AutoSize = true;
            this.luigi_IncreaseDifficulty.Location = new System.Drawing.Point(12, 471);
            this.luigi_IncreaseDifficulty.Name = "luigi_IncreaseDifficulty";
            this.luigi_IncreaseDifficulty.Size = new System.Drawing.Size(124, 17);
            this.luigi_IncreaseDifficulty.TabIndex = 104;
            this.luigi_IncreaseDifficulty.Text = "Increase Difficulty by";
            this.luigi_IncreaseDifficulty.UseVisualStyleBackColor = true;
            this.luigi_IncreaseDifficulty.CheckedChanged += new System.EventHandler(this.luigi_IncreaseDifficulty_CheckedChanged);
            // 
            // luigi_IncreaseLength
            // 
            this.luigi_IncreaseLength.AutoSize = true;
            this.luigi_IncreaseLength.Location = new System.Drawing.Point(12, 494);
            this.luigi_IncreaseLength.Name = "luigi_IncreaseLength";
            this.luigi_IncreaseLength.Size = new System.Drawing.Size(117, 17);
            this.luigi_IncreaseLength.TabIndex = 105;
            this.luigi_IncreaseLength.Text = "Increase Length by";
            this.luigi_IncreaseLength.UseVisualStyleBackColor = true;
            this.luigi_IncreaseLength.CheckedChanged += new System.EventHandler(this.luigi_IncreaseLength_CheckedChanged);
            // 
            // luigi_IncreaseDifficultyAmount
            // 
            this.luigi_IncreaseDifficultyAmount.Enabled = false;
            this.luigi_IncreaseDifficultyAmount.Location = new System.Drawing.Point(135, 469);
            this.luigi_IncreaseDifficultyAmount.Name = "luigi_IncreaseDifficultyAmount";
            this.luigi_IncreaseDifficultyAmount.Size = new System.Drawing.Size(48, 20);
            this.luigi_IncreaseDifficultyAmount.TabIndex = 103;
            // 
            // luigi_IncreaseLengthAmount
            // 
            this.luigi_IncreaseLengthAmount.Enabled = false;
            this.luigi_IncreaseLengthAmount.Location = new System.Drawing.Point(135, 492);
            this.luigi_IncreaseLengthAmount.Name = "luigi_IncreaseLengthAmount";
            this.luigi_IncreaseLengthAmount.Size = new System.Drawing.Size(48, 20);
            this.luigi_IncreaseLengthAmount.TabIndex = 106;
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(467, 743);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.panel1);
            this.Menu = this.mainMenu1;
            this.Name = "Form1";
            this.Text = "SharpNEAT";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.Form1_Closing);
            this.panel1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.gbxFile.ResumeLayout(false);
            this.gbxFile.PerformLayout();
            this.gbxCurrentStats.ResumeLayout(false);
            this.gbxCurrentStats.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.gbxSearchParameters.ResumeLayout(false);
            this.gbxSearchParameters.PerformLayout();
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.groupBox9.ResumeLayout(false);
            this.groupBox9.PerformLayout();
            this.groupBox10.ResumeLayout(false);
            this.groupBox10.PerformLayout();
            this.panel7.ResumeLayout(false);
            this.panel7.PerformLayout();
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.gbxLog.ResumeLayout(false);
            this.gbxLog.PerformLayout();
            this.ResumeLayout(false);

		}
		#endregion

		#region Main Method

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new Form1());
		}

		#endregion
		
		#region Private Methods

		private void InitialiseForm()
		{
		    experimentConfigInfoArray = ExperimentConfigUtils.ReadExperimentConfigCatalog();
		
			nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ",";

			txtLogWindow.BackColor = Color.White;
			PopulateDomainCombo();
			PopulateActivationFunctionCombo();
			cmbDomain.SelectedIndex=0;

			// Load default neatParameters.
			LoadNeatParameters(new NeatParameters());
            LoadNeatParameters(selectedExperiment.DefaultNeatParameters);

            if (selectedExperimentConfigInfo.Title == "Luigi")
                LoadLuigiParameters(TheLuigiExperiment.DefaultLuigiParameters);

			UpdateGuiState();

			// The progress graph form exists from this point on and is only
			// hidden/shown by the menu item. This allows the form to build up a history 
			// of data to show in the graphs.
            progressForm = new ProgressForm();
		}

		private void LoadNeatParameters(NeatParameters np)
		{
			txtParamPopulationSize.Text = np.populationSize.ToString();
			txtParamOffspringMutation.Text = np.pOffspringAsexual.ToString();
			txtParamOffspringCrossover.Text = np.pOffspringSexual.ToString();
			txtParamInterspeciesMating.Text = np.pInterspeciesMating.ToString();
			txtParamCompatThreshold.Text = np.compatibilityThreshold.ToString();
			txtParamCompatDisjointCoeff.Text = np.compatibilityDisjointCoeff.ToString();
			txtParamCompatExcessCoeff.Text = np.compatibilityExcessCoeff.ToString();
			txtParamCompatWeightDeltaCoeff.Text = np.compatibilityWeightDeltaCoeff.ToString();
			txtParamElitismProportion.Text = np.elitismProportion.ToString();
			txtParamSelectionProportion.Text = np.selectionProportion.ToString();
			txtParamTargetSpeciesCountMin.Text = np.targetSpeciesCountMin.ToString();
			txtParamTargetSpeciesCountMax.Text = np.targetSpeciesCountMax.ToString();
			txtParamSpeciesDropoffAge.Text = np.speciesDropoffAge.ToString();
			
			txtParamPruningBeginComplexityThreshold.Text = np.pruningPhaseBeginComplexityThreshold.ToString();
			txtParamPruningBeginFitnessStagnationThreshold.Text = np.pruningPhaseBeginFitnessStagnationThreshold.ToString();
			txtParamPruningEndComplexityStagnationThreshold.Text = np.pruningPhaseEndComplexityStagnationThreshold.ToString();

			txtParamMutateConnectionWeights.Text = np.pMutateConnectionWeights.ToString();
			txtParamMutateAddNode.Text = np.pMutateAddNode.ToString();
			txtParamMutateAddConnection.Text = np.pMutateAddConnection.ToString();
			txtParamMutateDeleteConnection.Text = np.pMutateDeleteConnection.ToString();
			txtParamMutateDeleteNeuron.Text = np.pMutateDeleteSimpleNeuron.ToString();

			txtParamConnectionWeightRange.Text = np.connectionWeightRange.ToString();
//			txtParamConnectionWeightMutationSigma.Text = np.connectionMutationSigma.ToString();
		}

        public void LoadLuigiParameters(LuigiParameters lp)
        {
            luigi_MaxFPS.Checked = lp.maximizeFps;
            luigi_enableGameViewer.Checked = lp.enableGameViewer;
            luigi_PortNumber.Text = lp.portNumber.ToString();
            luigi_RandomPort.Checked = lp.randomPort;
            luigi_RandomPortMin.Text = lp.randomPortMin.ToString();
            luigi_RandomPortMax.Text = lp.randomPortMax.ToString();
            luigi_EnableVisualization.Checked = lp.enableVisualization;
            luigi_StopSimAfterFirstWin.Checked = lp.stopSimAfterFirstWin;
            luigi_LuigiSmall.Checked = (lp.luigiMode == LuigiMode.LUIGI_SMALL);
            luigi_LuigiBig.Checked = (lp.luigiMode == LuigiMode.LUIGI_BIG);
            luigi_LuigiFire.Checked = (lp.luigiMode == LuigiMode.LUIGI_FIRE);
            luigi_Overground.Checked = (lp.levelMode == LevelMode.OVERGROUND);
            luigi_Underground.Checked = (lp.levelMode == LevelMode.UNDERGROUND);
            luigi_Castle.Checked = (lp.levelMode == LevelMode.CASTLE);
            luigi_Random.Checked = (lp.levelMode == LevelMode.RANDOM);
            luigi_LevelDifficulty.Text = lp.levelDifficulty.ToString();
            luigi_LevelLength.Text = lp.levelLength.ToString();
            luigi_TimeLimit.Text = lp.timeLimit.ToString();
            luigi_NumberOfRuns.Text = lp.numberOfRuns.ToString();
            luigi_LevelRandomizationSeed.Text = lp.levelRandomizationSeed.ToString();
            luigi_UseRandomSeed.Checked = lp.useRandomSeed;
            luigi_LevelRandomizationSeedMin.Text = lp.levelRandomizationSeedMin.ToString();
            luigi_LevelRandomizationSeedMax.Text = lp.levelRandomizationSeedMax.ToString();
            luigi_FitnessDistance.Text = lp.fitnessDistanceCoefficient.ToString();
            luigi_FitnessTime.Text = lp.fitnessTimeCoefficient.ToString();
            luigi_FitnessCoin.Text = lp.fitnessCoinCoefficient.ToString();
            luigi_FitnessLuigiSize.Text = lp.fitnessLuigiSizeCoefficient.ToString();
            luigi_FitnessKills.Text = lp.fitnessKillsCoefficient.ToString();
            luigi_FitnessPowerups.Text = lp.fitnessPowerupsCoefficient.ToString();
            luigi_FitnessBricks.Text = lp.fitnessBricksCoefficient.ToString();
            luigi_FitnessVictoryBonus.Text = lp.fitnessVictoryBonus.ToString();
            luigi_FitnessSuicidePenalty.Text = lp.fitnessSuicidePenalty.ToString();
            luigi_FitnessJumpPenalty.Text = lp.fitnessJumpPenalty.ToString();
            luigi_FitnessStopThreshold.Text = lp.fitnessStopThreshold.ToString();
            luigi_FitnessUseStopThreshold.Checked = lp.fitnessUseStopThreshold;
            luigi_GenerationStopThreshold.Text = lp.generationStopThreshold.ToString();
            luigi_UseGenerationStopThreshold.Checked = lp.useGenerationStopThreshold;
            luigi_MapZ0.Checked = (lp.zMap == ZMode.Z0);
            luigi_MapZ1.Checked = (lp.zMap == ZMode.Z1);
            luigi_MapZ2.Checked = (lp.zMap == ZMode.Z2);
            luigi_EnemyZ0.Checked = (lp.zEnemy == ZMode.Z0);
            luigi_EnemyZ1.Checked = (lp.zEnemy == ZMode.Z1);
            luigi_EnemyZ2.Checked = (lp.zEnemy == ZMode.Z2);
            //luigi_UseJumpScript2.Checked = lp.useJumpScript;
            luigi_UseNoJumpScript.Checked = (lp.jumpScript == JumpScript.NONE);
            luigi_UseHybridJumpScript.Checked = (lp.jumpScript == JumpScript.HYBRID);
            luigi_UseFullJumpScript.Checked = (lp.jumpScript == JumpScript.FULL);
            luigi_UseInverseDistances.Checked = lp.useInverseDistances;
            luigi_VictorySound.Checked = lp.playSoundOnWin;
            luigi_IncreaseDifficulty.Checked = lp.increaseDifficulty;
            luigi_IncreaseDifficultyAmount.Text = lp.increaseDifficultyAmount.ToString();
            luigi_IncreaseLength.Checked = lp.increaseLength;
            luigi_IncreaseLengthAmount.Text = lp.increaseLengthAmount.ToString();
        }
	
		private void PopulateDomainCombo()
		{
		    foreach (ExperimentConfigInfo eci in experimentConfigInfoArray)
		    {
		        cmbDomain.Items.Add(new ListItem("", eci.Title, eci));
		    }
		}

		private void PopulateActivationFunctionCombo()
		{
			cmbExperimentActivationFn.Items.Add(new ListItem("", "Reduced Sigmoid", new ReducedSigmoid()));
			cmbExperimentActivationFn.Items.Add(new ListItem("", "Plain Sigmoid", new PlainSigmoid()));
			cmbExperimentActivationFn.Items.Add(new ListItem("", "Steepened Sigmoid", new SteepenedSigmoid()));
			cmbExperimentActivationFn.Items.Add(new ListItem("", "Inv-Abs [FAST]", new InverseAbsoluteSigmoid()));
			cmbExperimentActivationFn.Items.Add(new ListItem("", "Sigmoid Approximation [FAST]", new SigmoidApproximation()));
			cmbExperimentActivationFn.Items.Add(new ListItem("", "Steepened Sigmoid Approx. [FAST]", new SteepenedSigmoidApproximation()));
			cmbExperimentActivationFn.Items.Add(new ListItem("", "Step Function", new StepFunction()));
            cmbExperimentActivationFn.Items.Add(new ListItem("", "Linear", new Linear()));
        }

		private void SearchThreadMethod()
		{
			// Keep track of when the last GUI update occured. And how many evalulations had occured at that time.
			long lastUpdateDateTimeTick=1;
			ulong lastUpdateGeneration=0;
			ulong lastUpdateEvaluationCount=0;

			double previousBestFitness=0.0;
			string previousEvaluatorStateMsg = string.Empty;

			try
			{
				for(;;)
				{
					if(stopSearchSignal)
					{
						searchState = SearchStateEnum.Paused;
						BeginInvoke(new MethodInvoker(UpdateGuiState));

						stopSearchSignal = false;	// reset flag.
						searchThread.Suspend();
					}

                    // JAT added
                    // JAT TODO - see if I can find out if luigi parameters changed.  Pass to ea.performonegeneration for performance boost.
                    // We can force all individuals to evaluate even if they have evaluated before, in case environment changes.

                    int currentLevelDifficulty = luigiParameters.levelDifficulty;
                    int currentLevelLength = luigiParameters.levelLength;
                    int guiLevelDifficulty = int.Parse(luigi_LevelDifficulty.GetControlPropertyThreadSafe("Text") as string);
                    int guiLevelLength = int.Parse(luigi_LevelLength.GetControlPropertyThreadSafe("Text") as string);
                    bool userModifiedDifficulty = (currentLevelDifficulty != guiLevelDifficulty);
                    bool userModifiedLength = (currentLevelLength != guiLevelLength);
                    bool autoUpdateDifficulty = (luigiParameters.shouldIncreaseDifficultyOrLength && luigiParameters.increaseDifficulty);
                    bool autoUpdateLength = (luigiParameters.shouldIncreaseDifficultyOrLength && luigiParameters.increaseLength);

                    luigiParameters.UpdateRandomSeed();

                    if (userModifiedDifficulty && autoUpdateDifficulty)
                    {
                        luigiParameters.levelDifficulty = int.Parse(luigi_LevelDifficulty.Text) - luigiParameters.increaseDifficultyAmount;
                    }
                    else if (userModifiedDifficulty)
                    {
                        luigiParameters.levelDifficulty = guiLevelDifficulty;
                    }
                    if (userModifiedLength && autoUpdateLength)
                    {
                        luigiParameters.levelLength = int.Parse(luigi_LevelLength.Text) - luigiParameters.increaseLengthAmount;
                    }
                    else if (userModifiedLength)
                    {
                        luigiParameters.levelLength = guiLevelLength;
                    }
                    luigiParameters.UpdateString(false);
                    luigi_LevelDifficulty.SetControlPropertyThreadSafe("Text", luigiParameters.levelDifficulty.ToString());
                    luigi_LevelLength.SetControlPropertyThreadSafe("Text", luigiParameters.levelLength.ToString());

                    GetUserLuigiParameters();

					// One generation.
					ea.PerformOneGeneration();

					//----- Determine if a GUI update is due.
					bool bUpdateDue=false;
					long tickDelta =  DateTime.Now.Ticks - lastUpdateDateTimeTick;
					if(updateMode)
					{	// Timebased updates.
						if(tickDelta > updateFreqTicks)
							bUpdateDue = true;
					}
					else
					{	// Generation based updates.
						if((ea.Generation - lastUpdateGeneration) >= updateFreqGens)
							bUpdateDue = true;
					}

					if(bUpdateDue)
					{
						// Calculate some stats and update the GUI to show them.
						float evaluationsDelta = ea.PopulationEvaluator.EvaluationCount - lastUpdateEvaluationCount;
						evaluationsPerSec = (int)(evaluationsDelta / ((float)tickDelta/10000000F));
						Invoke(new MethodInvoker(NotifyGuiUpdateRequired));

						// Write log entry if we are required to.
						if(logWriter!=null)
						{
							logWriter.WriteLine(ea.Generation.ToString() + ',' + 
								((DateTime.Now.Ticks-ticksAtSearchStart)*0.0000001).ToString("0.00000") + ',' + 
								ea.PopulationEvaluator.EvaluatorStateMessage + ',' + 
								ea.BestGenome.Fitness.ToString("0.00") + ',' +
								ea.Population.MeanFitness.ToString("0.00") + ',' +
								ea.Population.SpeciesTable.Count.ToString() + ',' +
								ea.NeatParameters.compatibilityThreshold.ToString("0.00") + ',' +
								((NeatGenome)ea.BestGenome).NeuronGeneList.Count.ToString() + ',' +
								((NeatGenome)ea.BestGenome).ConnectionGeneList.Count.ToString() + ',' +
								(ea.Population.TotalNeuronCount / ea.Population.GenomeList.Count).ToString("0.00") + ',' +
								(ea.Population.TotalConnectionCount / ea.Population.GenomeList.Count).ToString("0.00") + ',' +
								(ea.Population.AvgComplexity.ToString("0.00")));
						}

						// Store/update the lastUpdate* varaibles ready for the next loop.
						lastUpdateDateTimeTick = DateTime.Now.Ticks;
						lastUpdateGeneration = ea.Generation;
						lastUpdateEvaluationCount = ea.PopulationEvaluator.EvaluationCount;
					}

					//----- Check if we should save the best genome.
					if(		(ea.PopulationEvaluator.EvaluatorStateMessage != previousEvaluatorStateMsg)
						||	(ea.BestGenome.Fitness > previousBestFitness)
						||	(ea.PopulationEvaluator.BestIsIntermediateChampion))
					{
						//TODO: Technically this is not thread safe.
						if(chkFileSaveGenomeOnImprovement.Checked)
						{
							string filename = txtFileBaseName.Text + '_' + ea.PopulationEvaluator.EvaluatorStateMessage
								+ '_' + ea.BestGenome.Fitness.ToString("0.00", nfi)
								+ '_' + DateTime.Now.ToString("yyMMdd_hhmmss")
								+ (ea.PopulationEvaluator.BestIsIntermediateChampion ? "_champ" : "")
								+ ".xml";

							SaveBestGenome(filename);
						}

						previousBestFitness = ea.BestGenome.Fitness;
						previousEvaluatorStateMsg = ea.PopulationEvaluator.EvaluatorStateMessage;
					}

                    if (IsLuigiExperiment)
                    {
                        if ((luigiParameters.useGenerationStopThreshold && ea.Generation >= luigiParameters.generationStopThreshold) ||
                            (luigiParameters.fitnessUseStopThreshold && ea.BestGenome.Fitness > luigiParameters.fitnessStopThreshold))
                            stopSearchSignal = true;
                    }
				}
			}
			catch(Exception ex)
			{	
				if(ex is ThreadAbortException)
				{	// This is expected.
					return;
				}

				// Something went wrong! Usually an error within a plugged-in evaluation scheme.
				// Write entry to the application log.
				EventLog.WriteEntry("SharpNeat.exe", ex.ToString());
				
				// Report the exception to the log window.
				SafeLogMessage("\r\n" + ex.ToString());

				// Update the GUI state so that the user can re-start an experiment or save the population to file, etc.
				searchState = SearchStateEnum.Paused;
				BeginInvoke(new MethodInvoker(UpdateGuiState));

				stopSearchSignal = false;	// reset flag.
				searchThread.Suspend();
			}
		}

		private void NotifyGuiUpdateRequired()
		{
			UpdateStats();
			NotifyNetworkVisualization();
			progressForm.Update(ea);
			
			if(experimentView != null)
			{
				experimentView.RefreshView(GenomeDecoder.DecodeToFloatFastConcurrentNetwork((NeatGenome)ea.BestGenome, selectedActivationFunction));
			}
		}

		
		private void NotifyNetworkVisualization()
		{
			if(bestGenomeForm!=null)
			{
				bestGenomeForm.SetBestGenome((NeatGenome)ea.BestGenome, ea.Generation);
			}
			if(speciesForm!=null)
			{
				speciesForm.Update(ea);
			}
		}

		private string storedLogMessage=null;
		private void SafeLogMessage(string msg)
		{
			if(this.InvokeRequired)
			{
				storedLogMessage = msg;
				this.BeginInvoke(new MethodInvoker(SafeLogMessage));
				return;
			}
			LogMessage(msg);
		}

		private void SafeLogMessage()
		{
			LogMessage(storedLogMessage);
		}

		private void LogMessage(string msg)
		{
			txtLogWindow.AppendText(msg + "\r\n");
	
			if(txtLogWindow.Text.Length > 20000)
			{
				txtLogWindow.Text = txtLogWindow.Text.Substring(txtLogWindow.Text.Length-18000, 18000);
				txtLogWindow.ScrollToCaret();
			}
		}

		private void UpdateStats()
		{
			LogMessage("gen=" + ea.Generation + 
				", bestFitness=" + ea.BestGenome.Fitness.ToString("0.00") +
				", meanFitness=" + pop.MeanFitness.ToString("0.00") +
				", species=" + pop.SpeciesTable.Count + 
				", evaluatorMsg=" + ea.PopulationEvaluator.EvaluatorStateMessage);

			txtStatsGeneration.Text = ea.Generation.ToString();
			txtStatsBest.Text = ea.BestGenome.Fitness.ToString("0.000000");
			txtStatsMean.Text = pop.MeanFitness.ToString("0.000000");
			txtStatsSpeciesCount.Text = pop.SpeciesTable.Count.ToString();
			txtStatsCompatibilityThreshold.Text = ea.NeatParameters.compatibilityThreshold.ToString("0.0");
			txtStatsEvaluatorStateMsg.Text = ea.PopulationEvaluator.EvaluatorStateMessage;
			txtStatsTotalEvaluations.Text = ea.PopulationEvaluator.EvaluationCount.ToString();
			txtStatsEvaluationsPerSec.Text = evaluationsPerSec.ToString();

			NeatGenome bestGenome = (NeatGenome)ea.BestGenome;
			txtStatsBestGenomeLength.Text = "N " + bestGenome.NeuronGeneList.Count.ToString() + " / " +
				"C " + bestGenome.ConnectionGeneList.Count.ToString();
	
			txtStatsMeanGenomeLength.Text = "N " +  ((double)ea.Population.TotalNeuronCount / (double)ea.Population.GenomeList.Count).ToString("0.00") + " / " +
				"C " + ((double)ea.Population.TotalConnectionCount / (double)ea.Population.GenomeList.Count).ToString("0.00");

			if(ea.IsInPruningMode)
			{
				txtStatsMode.Text = "Pruning";
				txtStatsMode.BackColor = Color.Red;
			}
			else
			{
				txtStatsMode.Text = "Complexifying";
				txtStatsMode.BackColor = Color.FromKnownColor(KnownColor.Control);
			}
		}

		/// <summary>
		/// Read NeatParameters from the UI.
		/// </summary>
		/// <returns></returns>
		private NeatParameters GetUserNeatParameters()
		{
			NeatParameters np = new NeatParameters();

			np.populationSize = int.Parse(txtParamPopulationSize.Text);
			np.pOffspringAsexual = double.Parse(txtParamOffspringMutation.Text);
			np.pOffspringSexual = double.Parse(txtParamOffspringCrossover.Text);
			np.pInterspeciesMating = double.Parse(txtParamInterspeciesMating.Text);
			np.compatibilityThreshold = double.Parse(txtParamCompatThreshold.Text);
			np.compatibilityDisjointCoeff = double.Parse(txtParamCompatDisjointCoeff.Text);
			np.compatibilityExcessCoeff = double.Parse(txtParamCompatExcessCoeff.Text);
			np.compatibilityWeightDeltaCoeff = double.Parse(txtParamCompatWeightDeltaCoeff.Text);
			np.elitismProportion = double.Parse(txtParamElitismProportion.Text);
			np.selectionProportion = double.Parse(txtParamSelectionProportion.Text);
			np.targetSpeciesCountMin = int.Parse(txtParamTargetSpeciesCountMin.Text);
			np.targetSpeciesCountMax = int.Parse(txtParamTargetSpeciesCountMax.Text);
			np.speciesDropoffAge = int.Parse(txtParamSpeciesDropoffAge.Text);

			np.pruningPhaseBeginComplexityThreshold = float.Parse(txtParamPruningBeginComplexityThreshold.Text);
			np.pruningPhaseBeginFitnessStagnationThreshold = int.Parse(txtParamPruningBeginFitnessStagnationThreshold.Text);
			np.pruningPhaseEndComplexityStagnationThreshold = int.Parse(txtParamPruningEndComplexityStagnationThreshold.Text);

			np.pMutateConnectionWeights = double.Parse(txtParamMutateConnectionWeights.Text);
			np.pMutateAddNode = double.Parse(txtParamMutateAddNode.Text);
			np.pMutateAddConnection = double.Parse(txtParamMutateAddConnection.Text);

			np.pMutateDeleteConnection = double.Parse(txtParamMutateDeleteConnection.Text);
			np.pMutateDeleteSimpleNeuron = double.Parse(txtParamMutateDeleteNeuron.Text);
			
			np.connectionWeightRange = double.Parse(txtParamConnectionWeightRange.Text);

            np.allowRecurrence = selectedExperiment.DefaultNeatParameters.allowRecurrence;

			return np;
		}

        private LuigiParameters GetUserLuigiParameters()
        {
            LuigiParameters lp = luigiParameters;
            lp.maximizeFps = luigi_MaxFPS.Checked;
            lp.enableGameViewer = luigi_enableGameViewer.Checked;
            lp.portNumber = int.Parse(luigi_PortNumber.Text);
            lp.randomPort = luigi_RandomPort.Checked;
            lp.randomPortMin = int.Parse(luigi_RandomPortMin.Text);
            lp.randomPortMax = int.Parse(luigi_RandomPortMax.Text);
            lp.enableVisualization = luigi_EnableVisualization.Checked;
            lp.stopSimAfterFirstWin = luigi_StopSimAfterFirstWin.Checked;;
            if(luigi_LuigiSmall.Checked) lp.luigiMode = LuigiMode.LUIGI_SMALL;
            if(luigi_LuigiBig.Checked) lp.luigiMode = LuigiMode.LUIGI_BIG;
            if(luigi_LuigiFire.Checked) lp.luigiMode = LuigiMode.LUIGI_FIRE;
            if(luigi_Overground.Checked) lp.levelMode = LevelMode.OVERGROUND;
            if(luigi_Underground.Checked) lp.levelMode = LevelMode.UNDERGROUND;
            if(luigi_Castle.Checked) lp.levelMode = LevelMode.CASTLE;
            if(luigi_Random.Checked) lp.levelMode = LevelMode.RANDOM;
            lp.levelDifficulty = int.Parse(luigi_LevelDifficulty.Text);
            lp.levelLength = int.Parse(luigi_LevelLength.Text);
            lp.timeLimit = int.Parse(luigi_TimeLimit.Text);
            lp.numberOfRuns = int.Parse(luigi_NumberOfRuns.Text);
            lp.levelRandomizationSeed = int.Parse(luigi_LevelRandomizationSeed.Text);
            lp.useRandomSeed = luigi_UseRandomSeed.Checked;
            lp.levelRandomizationSeedMin = int.Parse(luigi_LevelRandomizationSeedMin.Text);
            lp.levelRandomizationSeedMax = int.Parse(luigi_LevelRandomizationSeedMax.Text);
            lp.fitnessDistanceCoefficient = double.Parse(luigi_FitnessDistance.Text);
            lp.fitnessTimeCoefficient = double.Parse(luigi_FitnessTime.Text);
            lp.fitnessCoinCoefficient = double.Parse(luigi_FitnessCoin.Text);
            lp.fitnessLuigiSizeCoefficient = double.Parse(luigi_FitnessLuigiSize.Text);
            lp.fitnessKillsCoefficient = double.Parse(luigi_FitnessKills.Text);
            lp.fitnessPowerupsCoefficient = double.Parse(luigi_FitnessPowerups.Text);
            lp.fitnessBricksCoefficient = double.Parse(luigi_FitnessBricks.Text);
            lp.fitnessVictoryBonus = double.Parse(luigi_FitnessVictoryBonus.Text);
            lp.fitnessSuicidePenalty = double.Parse(luigi_FitnessSuicidePenalty.Text);
            lp.fitnessJumpPenalty = double.Parse(luigi_FitnessJumpPenalty.Text);
            lp.fitnessStopThreshold = double.Parse(luigi_FitnessStopThreshold.Text);
            lp.fitnessUseStopThreshold = luigi_FitnessUseStopThreshold.Checked;
            lp.generationStopThreshold = int.Parse(luigi_GenerationStopThreshold.Text);
            lp.useGenerationStopThreshold = luigi_UseGenerationStopThreshold.Checked;
            if (luigi_MapZ0.Checked) lp.zMap = ZMode.Z0;
            if (luigi_MapZ1.Checked) lp.zMap = ZMode.Z1;
            if (luigi_MapZ2.Checked) lp.zMap = ZMode.Z2;
            if (luigi_EnemyZ0.Checked) lp.zEnemy = ZMode.Z0;
            if (luigi_EnemyZ1.Checked) lp.zEnemy = ZMode.Z1;
            if (luigi_EnemyZ2.Checked) lp.zEnemy = ZMode.Z2;
            //lp.useJumpScript = luigi_UseJumpScript2.Checked;
            if (luigi_UseNoJumpScript.Checked) lp.jumpScript = JumpScript.NONE;
            if (luigi_UseHybridJumpScript.Checked) lp.jumpScript = JumpScript.HYBRID;
            if (luigi_UseFullJumpScript.Checked) lp.jumpScript = JumpScript.FULL;
            lp.useInverseDistances = luigi_UseInverseDistances.Checked;
            lp.playSoundOnWin = luigi_VictorySound.Checked;
            lp.increaseDifficulty = luigi_IncreaseDifficulty.Checked;
            lp.increaseDifficultyAmount = int.Parse(luigi_IncreaseDifficultyAmount.Text);
            lp.increaseLength = luigi_IncreaseLength.Checked;
            lp.increaseLengthAmount = int.Parse(luigi_IncreaseLengthAmount.Text);

            //lp.parametersAsString = lp.ToString();
            lp.UpdateString(true);
            lp.UpdateRandomSeed();
            return lp;
        }

		/// <summary>
		/// Checks if the provided file location can be written too.
		/// If the file already exists then we check it's ReadOnly flag. If the flag is set then ask the user
		/// if they wish to overwrite the file - if they select yes then reset the file's RO attribute and return true.
		/// If the file does not exist then return true - the filename specified can be written to.
		/// </summary>
		/// <param name="file">The fileInfo object representing the filename we wish to check.</param>
		/// <returns>True if the file location provided can be written too. Otherwise returns false.</returns>
		private bool IsWriteableFile(FileInfo file)
		{
			file.Refresh();
			if(file.Exists)
			{
				// If the file exists and it is readonly ask the user whether it should be overwritten
				if(file.Exists && ((file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly))
				{
					// Retrieve Title and Message from resource file
					string sMessageTitle = "Read-Only File";
					string sMessage = @"The current file is read-only:
  %1
Do you wish to overwrite the file?";
					sMessage = sMessage.Replace("%1", file.FullName);

					// Display message and respond to selection
					DialogResult oResult = MessageBox.Show(
						this,
						sMessage, 
						sMessageTitle, 
						MessageBoxButtons.YesNo, 
						MessageBoxIcon.Question, 
						MessageBoxDefaultButton.Button2);
					switch((int)oResult)
					{
						case (int) DialogResult.Yes:
						{
							// Reverse ReadOnly status
							file.Attributes = file.Attributes ^ FileAttributes.ReadOnly;
						
							// File exists and has been made writable
							return true;
						}
						case (int) DialogResult.No:
						{
							return false;
						}
						default:
						{
							return false;
						}
					}
				}
			}

			// The provided filename either does not exist or it exists and is read/write.
			return true;
		}


		/// <summary>
		/// Ask the user for a filename / path.
		/// </summary>
		private string SelectFileToSave(string dialogTitle)
		{
			//----- Save the XmlDocument to the file syatem.
			SaveFileDialog oDialog = new SaveFileDialog();
			oDialog.AddExtension = true;
			oDialog.DefaultExt = "xml";
			oDialog.Title = dialogTitle;
			oDialog.RestoreDirectory = true;

			// Show Open Dialog and Respond to OK
			if(oDialog.ShowDialog() == DialogResult.OK)
				return oDialog.FileName;
			else
				return string.Empty;
		}

		/// <summary>
		/// Ask the user for a filename / path.
		/// </summary>
		private string SelectFileToOpen(string dialogTitle)
		{
			//----- Save the XmlDocument to the file syatem.
			OpenFileDialog oDialog = new OpenFileDialog();
			oDialog.AddExtension = true;
			oDialog.DefaultExt = "xml";
			oDialog.Title = dialogTitle;
			oDialog.RestoreDirectory = true;

			// Show Open Dialog and Respond to OK
			if(oDialog.ShowDialog() == DialogResult.OK)
				return oDialog.FileName;
			else
				return string.Empty;
		}

		private void SaveBestNetwork()
		{
			string filename = SelectFileToSave("Save best genome as network XML");
			if(filename != string.Empty)
				SaveBestNetwork(filename);
		}

		private void SaveBestNetwork(string filename)
		{
			if(ea.BestGenome==null)
				return;

			//----- Determine the current experiment.
            /* NOTE RJM: Assumes that an experiment was already selected in event
               cmbDomain_SelectedIndexChanged
             */
            IExperiment experiment = selectedExperiment;

			//----- Write the genome to an XmlDocument.
			XmlDocument doc = new XmlDocument();
			XmlNetworkWriterStatic.Write(doc, (NeatGenome)ea.BestGenome, selectedActivationFunction);
			FileInfo oFileInfo = new FileInfo(filename);

			if(IsWriteableFile(oFileInfo))
				doc.Save(oFileInfo.FullName);
			else
				return;
		}


		/// <summary>
		/// Ask the user for a filename / path.
		/// </summary>
		private void SaveBestGenome()
		{
			string filename = SelectFileToSave("Save best genome.");
			if(filename != string.Empty)
				SaveBestGenome(filename);
		}

		private void SaveBestGenome(string filename)
		{
			if(ea.BestGenome==null)
				return;

			//----- Determine the current experiment.
            /* NOTE RJM: Assumes that an experiment was already selected in event
               cmbDomain_SelectedIndexChanged
             */
            IExperiment experiment = selectedExperiment;

			//----- Write the genome to an XmlDocument.
			XmlDocument doc = new XmlDocument();
			XmlGenomeWriterStatic.Write(doc, (NeatGenome)ea.BestGenome, selectedActivationFunction);
			FileInfo oFileInfo = new FileInfo(filename);

			if(IsWriteableFile(oFileInfo))
				doc.Save(oFileInfo.FullName);
			else
				return;
		}

		private void SavePopulation()
		{
			string filename = SelectFileToSave("Save Population.");
			if(filename != string.Empty)
				SavePopulation(filename);
		}

		private void SavePopulation(string filename)
		{
			if(pop==null)
				return;

			//----- Determine the current experiment.
            /* NOTE RJM: Assumes that an experiment was already selected in event
               cmbDomain_SelectedIndexChanged
             */
            IExperiment experiment = selectedExperiment;

			//----- Write the population to an XmlDocument.
			XmlDocument doc = new XmlDocument();
			XmlPopulationWriter.Write(doc, pop, selectedActivationFunction);
			FileInfo oFileInfo = new FileInfo(filename);

			if(IsWriteableFile(oFileInfo))
				doc.Save(oFileInfo.FullName);
			else
				return;
		}

		private void LoadPopulation()
		{
			string filename = SelectFileToOpen("Load Population");
			if(filename == string.Empty)
				return;

			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(filename);
				IGenomeReader genomeReader = new XmlNeatGenomeReader();
				pop = XmlPopulationReader.Read(doc, genomeReader, new IdGeneratorFactory());
			}
			catch(Exception e)
			{
				MessageBox.Show("Problem loading population. \n" + e.Message);
				pop = null;
				return;
			}

			if(!pop.IsCompatibleWithNetwork(selectedExperiment.InputNeuronCount,
				selectedExperiment.OutputNeuronCount))
			{	
				MessageBox.Show(@"At least one genome in the population is incompatible with the currently selected experiment. Check the number of input/output neurons.");
				pop = null;
			}
			else
			{	// Population is OK.
				txtParamPopulationSize.Text = pop.GenomeList.Count.ToString();
			}
		}

		private void InititialisePopulationFromSeedGenome()
		{
			string filename = SelectFileToOpen("Load Seed Genome");
			if(filename == string.Empty)
				return;

			NeatGenome seedGenome=null;
			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(filename);
				seedGenome = XmlNeatGenomeReaderStatic.Read(doc);	
			}
			catch(Exception e)
			{
				MessageBox.Show("Problem loading genome. \n" + e.Message);
				pop = null;
				return;
			}

			if(!seedGenome.IsCompatibleWithNetwork(selectedExperiment.InputNeuronCount,
				selectedExperiment.OutputNeuronCount))
			{
				MessageBox.Show(@"The genome is incompatible with the currently selected experiment. Check the number of input/output neurons.");
				pop = null;
			}
			else
			{
				NeatParameters neatParameters = GetUserNeatParameters();
				IdGeneratorFactory idGeneratorFactory = new IdGeneratorFactory();
				IdGenerator idGenerator = idGeneratorFactory.CreateIdGenerator(seedGenome);
				GenomeList genomeList = GenomeFactory.CreateGenomeList(	seedGenome,
					neatParameters.populationSize,
					neatParameters,
					idGenerator);

				pop = new Population(idGenerator, genomeList);
			}
		}


		private void InititialisePopulationFromSeedPopulation()
		{
			string filename = SelectFileToOpen("Load Seed Population");
			if(filename == string.Empty)
				return;

			Population seedPopulation=null;
			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(filename);
				IGenomeReader genomeReader = new XmlNeatGenomeReader();
				seedPopulation = XmlPopulationReader.Read(doc, genomeReader, new IdGeneratorFactory());
			}
			catch(Exception e)
			{
				MessageBox.Show("Problem loading population. \n" + e.Message);
				pop = null;
				return;
			}

			if(!seedPopulation.IsCompatibleWithNetwork(selectedExperiment.InputNeuronCount,
				selectedExperiment.OutputNeuronCount))
			{	
				MessageBox.Show(@"At least one genome in the population is incompatible with the currently selected experiment. Check the number of input/output neurons.");
				pop = null;
			}
			else
			{
				NeatParameters neatParameters = GetUserNeatParameters();
				IdGeneratorFactory idGeneratorFactory = new IdGeneratorFactory();
				IdGenerator idGenerator = idGeneratorFactory.CreateIdGenerator(seedPopulation.GenomeList);
				GenomeList genomeList = GenomeFactory.CreateGenomeList(	seedPopulation,
					neatParameters.populationSize,
					neatParameters,
					idGenerator);

				pop = new Population(idGenerator, genomeList);
			}
		}

		private void UpdateGuiState()
		{
			if(pop==null)
			{	
				// Search cannot be started without a population.
				btnSearchStart.Enabled = false;
				btnSearchStop.Enabled = false;
				btnSearchReset.Enabled = false;

				cmbDomain.Enabled = true;
				cmbExperimentActivationFn.Enabled = true;
				btnLoadDefaults.Enabled = true;
				gbxSearchParameters.Enabled = true;
				gbxFile.Enabled=true;

				mnuFileSave.Enabled = false;
				mnuInitPop.Enabled = true;
			}
			else
			{	
				// There is a population. Check search state.
				switch(searchState)
				{
					case SearchStateEnum.Reset:
						btnSearchStart.Enabled = true;
						btnSearchStop.Enabled = false;
						btnSearchReset.Enabled = false;

						cmbDomain.Enabled = true;
						cmbExperimentActivationFn.Enabled = true;
						btnLoadDefaults.Enabled = true;
						gbxSearchParameters.Enabled = true;
						gbxFile.Enabled=true;

						mnuFileSave.Enabled = false;
						mnuInitPop.Enabled = false;
						break;
					
					case SearchStateEnum.Paused:
						btnSearchStart.Enabled = true;
						btnSearchStop.Enabled = false;
						btnSearchReset.Enabled = true;

						cmbDomain.Enabled = false;
						cmbExperimentActivationFn.Enabled = false;
						btnLoadDefaults.Enabled = false;
						gbxSearchParameters.Enabled = false;
						gbxFile.Enabled=false;

						mnuFileSave.Enabled = true;
						mnuInitPop.Enabled = false;
						break;

					case SearchStateEnum.Running:
						btnSearchStart.Enabled = false;
						btnSearchStop.Enabled = true;
						btnSearchReset.Enabled = false;

						cmbDomain.Enabled = false;
						cmbExperimentActivationFn.Enabled = false;
						btnLoadDefaults.Enabled = false;
						gbxSearchParameters.Enabled = false;
						gbxFile.Enabled=false;

						mnuFileSave.Enabled = false;
						mnuInitPop.Enabled = false;
						break;
				}
			}
			
		}

		private void FlushAndCloseLogFile()
		{
			// Ensure any log data is written to file before the application terminates.
			if(logWriter != null)
			{
				logWriter.Close();
				logWriter = null;
			}
		}

		private void ClearUpdateFreqMenus()
		{
			mnuViewUpdateFrequency1Sec.Checked = false;
			mnuViewUpdateFrequency2Sec.Checked = false;
			mnuViewUpdateFrequency5Sec.Checked = false;
			mnuViewUpdateFrequency10Sec.Checked = false;
			mnuViewUpdateFrequency1Gen.Checked = false;
			mnuViewUpdateFrequency2Gen.Checked = false;
			mnuViewUpdateFrequency5Gen.Checked = false;
			mnuViewUpdateFrequency10Gen.Checked = false;
		}

		#endregion

		#region Event Handlers
		
		#region File Menu

		private void mnuFileSaveBestAsNetwork_Click(object sender, System.EventArgs e)
		{
			SaveBestNetwork();
		}

		private void mnuFileSaveBestAsGenome_Click(object sender, System.EventArgs e)
		{
			SaveBestGenome();
		}


		private void mnuFileSavePopulation_Click(object sender, System.EventArgs e)
		{
			SavePopulation();
		}

		#endregion

		#region Initialize Population Menu

		private void mnuInitPopLoadPopulation_Click(object sender, System.EventArgs e)
		{
			LoadPopulation();
			UpdateGuiState();
		}

		private void mnuInitPopLoadSeedGenome_Click(object sender, System.EventArgs e)
		{
			InititialisePopulationFromSeedGenome();
			UpdateGuiState();
		}

		private void mnuInitPopLoadSeedPopulation_Click(object sender, System.EventArgs e)
		{
			InititialisePopulationFromSeedPopulation();
			UpdateGuiState();
		}

		private void mnuInitPopAutoGenerate_Click(object sender, System.EventArgs e)
		{
			ListItem listItem = (ListItem)cmbDomain.SelectedItem;
			/* NOTE RJM: Assumes that an experiment was already selected in event
			   cmbDomain_SelectedIndexChanged
			 */
			IExperiment experiment = selectedExperiment;

			NeatParameters neatParameters = GetUserNeatParameters();
			AutoGeneratePopulationForm form = new AutoGeneratePopulationForm(
												neatParameters, 
												selectedExperiment.InputNeuronCount,
												selectedExperiment.OutputNeuronCount);
			form.ShowDialog(this);
			if(form.DialogResult==DialogResult.OK)
			{
				pop = form.Population;
			}

			UpdateGuiState();
		}

		#endregion

		#region Search Buttons

		private void btnSearchStart_Click(object sender, System.EventArgs e)
		{
			searchState = SearchStateEnum.Running;
			UpdateGuiState();

			if(searchThread==null)
			{
                /* NOTE RJM: Assumes that an experiment was already selected in event
                   cmbDomain_SelectedIndexChanged
                 */
                IExperiment experiment = selectedExperiment;

				//--- Generate a new population.
				NeatParameters neatParameters = GetUserNeatParameters();
				IdGenerator idGenerator = new IdGenerator();

                if(IsLuigiExperiment)
                    TheLuigiExperiment.ResetEvaluator(selectedActivationFunction, GetUserLuigiParameters());
                else
                    experiment.ResetEvaluator(selectedActivationFunction);

				//--- Create a new EvolutionAlgorithm.
				ea = new EvolutionAlgorithm(pop, experiment.PopulationEvaluator, neatParameters);
				ea.IsPruningModeEnabled = chkParamPruningModeEnabled.Checked;
				ea.IsConnectionWeightFixingEnabled = chkParamEnableConnectionWeightFixing.Checked;
			
				//--- Create a log file if necessary.
				if(chkFileWriteLog.Checked)
				{
					string filename = txtFileLogBaseName.Text + '_' + DateTime.Now.ToString("yyMMdd_hhmmss") + ".txt";
					logWriter = new StreamWriter(filename, true);
					logWriter.WriteLine("Gen, ClockTime, EvalStateMsg, BestFitness, MeanFitness, SpeciesCount, SpeciesCompatThreshold, BestGenomeNeuronCount, BestGenomeConnectionCount, PopMeanNeuronCount, PopMeanConnectionCount, MeanStructuresPerGenome");
				}

				stopSearchSignal=false; // reset this signal.
				searchThread = new Thread(new ThreadStart(SearchThreadMethod));
				searchThread.IsBackground = true;
				searchThread.Priority = ThreadPriority.BelowNormal;
				ticksAtSearchStart = DateTime.Now.Ticks;

				searchThread.Start();
			}
			else
			{
                if (IsLuigiExperiment)
                    GetUserLuigiParameters();
				searchThread.Resume();
			}
		}

		private void btnSearchStop_Click(object sender, System.EventArgs e)
		{
			// Don't stop the thread here. Send a signal to the thread to
			// stop when it is next convienient. This is primarily done
			// so that we always stop the search with a full population - during
			// PerformOneGeneration() the population size fluctuates.
			stopSearchSignal=true;
		}

		private void btnSearchReset_Click(object sender, System.EventArgs e)
		{
			if(searchThread!=null)
			{
				if((searchThread.ThreadState & System.Threading.ThreadState.Suspended) ==0)
				{	// User must stop thread first.
					return;
				}
				// Bug in .Net requires call to Resume before Abort.!
				searchThread.Resume();
				searchThread.Abort(); 

			}

			FlushAndCloseLogFile();

			searchThread = null;
			txtLogWindow.Text = string.Empty;
			pop = null;
			searchState = SearchStateEnum.Reset;
			UpdateGuiState();

			// Reset stats window.
			txtStatsGeneration.Text = string.Empty;
			txtStatsBest.Text = string.Empty;
			txtStatsMean.Text = string.Empty;
			txtStatsSpeciesCount.Text = string.Empty;
			txtStatsCompatibilityThreshold.Text = string.Empty;
			txtStatsEvaluatorStateMsg.Text = string.Empty;
			txtStatsTotalEvaluations.Text = string.Empty;

			// Reset visualization windows.
			if(progressForm!=null)
				progressForm.Reset();

			if(bestGenomeForm!=null)
				bestGenomeForm.Reset();

			if(speciesForm!=null)
				speciesForm.Reset();
		}

		#endregion

		#region Update Frequency Menu

		private void mnuViewUpdateFrequency1Sec_Click(object sender, System.EventArgs e)
		{
			updateFreqTicks=10000000; 
			updateMode=true;
			ClearUpdateFreqMenus();
			mnuViewUpdateFrequency1Sec.Checked = true;
		}

		private void mnuViewUpdateFrequency2Sec_Click(object sender, System.EventArgs e)
		{
			updateFreqTicks=20000000; 
			updateMode=true;
			ClearUpdateFreqMenus();
			mnuViewUpdateFrequency2Sec.Checked = true;
		}

		private void mnuViewUpdateFrequency5Sec_Click(object sender, System.EventArgs e)
		{
			updateFreqTicks=50000000; 
			updateMode=true;
			ClearUpdateFreqMenus();
			mnuViewUpdateFrequency5Sec.Checked = true;
		}

		private void mnuViewUpdateFrequency10Sec_Click(object sender, System.EventArgs e)
		{
			updateFreqTicks=100000000; 
			updateMode=true;
			ClearUpdateFreqMenus();
			mnuViewUpdateFrequency10Sec.Checked = true;
		}

		private void mnuViewUpdateFrequency1Gen_Click(object sender, System.EventArgs e)
		{
			updateFreqGens=1;
			updateMode=false;
			ClearUpdateFreqMenus();
			mnuViewUpdateFrequency1Gen.Checked = true;
		}

		private void mnuViewUpdateFrequency2Gen_Click(object sender, System.EventArgs e)
		{
			updateFreqGens=2;
			updateMode=false;
			ClearUpdateFreqMenus();
			mnuViewUpdateFrequency2Gen.Checked = true;
		}

		private void mnuViewUpdateFrequency5Gen_Click(object sender, System.EventArgs e)
		{
			updateFreqGens=5;
			updateMode=false;
			ClearUpdateFreqMenus();
			mnuViewUpdateFrequency5Gen.Checked = true;
		}

		private void mnuViewUpdateFrequency10Gen_Click(object sender, System.EventArgs e)
		{
			updateFreqGens=10;
			updateMode=false;
			ClearUpdateFreqMenus();
			mnuViewUpdateFrequency10Gen.Checked = true;
		}

		#endregion

		#region About Menu

		private void mnuAbout_Click(object sender, System.EventArgs e)
		{
			Form frmAboutBox = new AboutBox();
			frmAboutBox.ShowDialog(this);
		}

		#endregion

		#region Misc Buttons

		private void btnDomainExplanation_Click(object sender, System.EventArgs e)
		{
            /* NOTE RJM: Assumes that an experiment was already selected in event
               cmbDomain_SelectedIndexChanged
             */
            IExperiment experiment = selectedExperiment;

			MessageBox.Show(experiment.ExplanatoryText);
		}

		private void btnLoadDefaults_Click(object sender, System.EventArgs e)
		{
			// Load default neatParameters.
			LoadNeatParameters(selectedExperiment.DefaultNeatParameters);
            //if (selectedExperimentConfigInfo.Title == "Luigi")
            if(IsLuigiExperiment)
                LoadLuigiParameters(TheLuigiExperiment.DefaultLuigiParameters);
		}

		private void btnPlotFunction_Click(object sender, System.EventArgs e)
		{
            if(activationFunctionForm==null)
            {
                activationFunctionForm = new ActivationFunctionForm();
                activationFunctionForm.ActivationFunction = selectedActivationFunction;
                activationFunctionForm.Closed+=new EventHandler(activationFunctionForm_Closed);
                activationFunctionForm.Show();
            }
		}

		#endregion

		#region Domain / Experiment GroupBox Controls
		
        private void cmbDomain_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            // Discard any existing population - its probably invalid for this problem (input/output neuron counts).
            pop = null;

            // Store a reference to the selected experiment.
            ListItem listItem = (ListItem)cmbDomain.SelectedItem;
            selectedExperimentConfigInfo = (ExperimentConfigInfo)listItem.Data;
			
            // This object will get re-created every time the user switches 
            // options, so here's to garbage collection working.
			ObjectHandle objectHandle = Activator.CreateInstanceFrom(selectedExperimentConfigInfo.AssemblyUrl, selectedExperimentConfigInfo.TypeName);
			selectedExperiment = (IExperiment)objectHandle.Unwrap();
			selectedExperiment.LoadExperimentParameters(selectedExperimentConfigInfo.ParameterTable);

//			selectedExperiment = (IExperiment)AppTools.RunSubApplication(selectedExperimentConfigInfo.ApplicationData);
//			selectedExperiment.LoadExperimentParameters(selectedExperimentConfigInfo.ParameterTable);

            if (selectedExperiment != null && IsLuigiExperiment && !this.tabControl1.Controls.Contains(this.tabPage3))
                this.tabControl1.Controls.Add(this.tabPage3);
            else if (selectedExperiment != null && !IsLuigiExperiment && this.tabControl1.Controls.Contains(this.tabPage3))
                this.tabControl1.Controls.Remove(this.tabPage3);

            txtDomainInputNeuronCount.Text = selectedExperiment.InputNeuronCount.ToString();
            txtDomainOutputNeuronCount.Text = selectedExperiment.OutputNeuronCount.ToString();

            SelectActivationFunction(selectedExperiment.SuggestedActivationFunction);

            // Close any open experiment view. It will likely be for a different experiment.
            if(experimentView!=null)
            {
                experimentView.Close();
            }

            // JAT AUTO GENERATE POPULATION
            /*
            LoadNeatParameters(selectedExperiment.DefaultNeatParameters);

            IExperiment experiment = selectedExperiment;

            NeatParameters neatParameters = GetUserNeatParameters();

            int size = selectedExperiment.DefaultNeatParameters.populationSize;
            float connectionProportion = 0.05f; // JAT TODO - user generated?

            IdGenerator idGenerator = new IdGenerator();

            GenomeList genomeList = GenomeFactory.CreateGenomeList(
                neatParameters,
                idGenerator,
                selectedExperiment.InputNeuronCount,
                selectedExperiment.OutputNeuronCount,
                connectionProportion,
                size);

            pop = new Population(idGenerator, genomeList);
            UpdateGuiState();
             */
        }

		private void cmbExperimentActivationFn_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// Store a reference to the selected activation function.
            // Store a reference to the selected activation function.
            ListItem listItem = (ListItem)cmbExperimentActivationFn.SelectedItem;
            selectedActivationFunction = (IActivationFunction)listItem.Data;
            txtExperimentActivationFn.Text = selectedActivationFunction.FunctionString;
        }

		private void SelectActivationFunction(IActivationFunction activationFn)
		{
		    // TODO RJM: See how this is affected by the recent change.
			foreach(ListItem listItem in cmbExperimentActivationFn.Items)
			{
				if(listItem.Data.GetType() == activationFn.GetType())
				{
					cmbExperimentActivationFn.SelectedItem = listItem;
					return;
				}
			}
		}

		#endregion

		#region Visualization Menu

		private void mnuVisualizationProgressGraph_Click(object sender, System.EventArgs e)
		{
			if(!progressForm.Visible)
			{
				progressForm.Show();
			}
		}

		private void mnuVisualizationBest_Click(object sender, System.EventArgs e)
		{
			if(bestGenomeForm==null)
			{
				bestGenomeForm = new BestGenomeForm();
				bestGenomeForm.Closed+=new EventHandler(bestGenomeForm_Closed);
				bestGenomeForm.Show();

				//TODO: Slightly dodgy. Chance of a threading problem?
				if(ea!=null && ea.BestGenome!=null)
					bestGenomeForm.SetBestGenome((NeatGenome)ea.BestGenome, ea.Generation);
			}
		}

		private void mnuVisualizationSpecies_Click(object sender, System.EventArgs e)
		{
			if(speciesForm==null)
			{
				speciesForm = new SpeciesForm();
				speciesForm.Closed+=new EventHandler(speciesForm_Closed);
				speciesForm.Show();

				if(ea!=null)
					speciesForm.Update(ea);
			}
		}

		private void mnuVisualizationExperiment_Click(object sender, System.EventArgs e)
		{
			// If there is already a view created then do nothing.
			if(experimentView!=null)
				return;

			// Just in case, test for null.
			if(selectedExperiment==null)
				return;

			// Ask the current experiment to create a view.
			experimentView = selectedExperiment.CreateExperimentView();

			// Some experiments may not have aview defined. Test for null.
			if(experimentView==null)
				return;

			// OK we have a view, so lets show it.
			experimentView.Closed +=new EventHandler(experimentView_Closed);
			experimentView.Show();
		}

		#endregion

		#region BestGenomeForm

		private void bestGenomeForm_Closed(object sender, EventArgs e)
		{
			bestGenomeForm=null;
		}

		private void speciesForm_Closed(object sender, EventArgs e)
		{
			speciesForm=null;
		}

        private void activationFunctionForm_Closed(object sender, EventArgs e)
        {
            activationFunctionForm=null;
        }
		
		#endregion

		#region ExperimentView

		private void experimentView_Closed(object sender, EventArgs e)
		{
			experimentView=null;
		}

		#endregion

		#region Search Parameters GroupBox Controls

		private void chkParamPruningModeEnabled_CheckedChanged(object sender, System.EventArgs e)
		{
			// If pruning mode is disabled then connection weight fixing cannot occur.
			if(chkParamPruningModeEnabled.Checked)
			{
				chkParamEnableConnectionWeightFixing.Enabled = true;
			}
			else
			{
				chkParamEnableConnectionWeightFixing.Enabled = false;
				chkParamEnableConnectionWeightFixing.Checked = false;
			}
		}

		#endregion

		#region Form Events

		private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			FlushAndCloseLogFile();
		}

		#endregion

		#endregion

        private void luigi_UseRandomSeed_CheckedChanged(object sender, EventArgs e)
        {
            bool boxChecked = (sender as CheckBox).Checked;
            luigi_LevelRandomizationSeed.Enabled = !boxChecked;
            luigi_LevelRandomizationSeedMin.Enabled = boxChecked;
            luigi_LevelRandomizationSeedMax.Enabled = boxChecked;
        }

        private void luigi_RandomPort_CheckedChanged(object sender, EventArgs e)
        {
            bool boxChecked = (sender as CheckBox).Checked;
            luigi_PortNumber.Enabled = !(sender as CheckBox).Checked;
            luigi_RandomPortMin.Enabled = boxChecked;
            luigi_RandomPortMax.Enabled = boxChecked;
        }

        private void luigi_FitnessUseStopThreshold_CheckedChanged(object sender, EventArgs e)
        {
            luigi_FitnessStopThreshold.Enabled = (sender as CheckBox).Checked;
        }

        private void luigi_UseGenerationStopThreshold_CheckedChanged(object sender, EventArgs e)
        {
            luigi_GenerationStopThreshold.Enabled = (sender as CheckBox).Checked;
        }

        private void luigi_EnableVisualization_CheckedChanged(object sender, EventArgs e)
        {
            luigiParameters.enableVisualization = (sender as CheckBox).Checked;
            luigiParameters.UpdateString(true);
        }

        private void luigi_NumberOfRuns_TextChanged(object sender, EventArgs e)
        {
            double boxText;
            if (Double.TryParse((sender as TextBox).Text, out boxText))
            {
                if (boxText <= 0)
                {
                    (sender as TextBox).Text = "1";
                }
                else if (boxText > 1)
                {
                    luigi_UseRandomSeed.Checked = true;
                    luigi_LevelRandomizationSeed.Enabled = false;
                    luigi_LevelRandomizationSeedMin.Enabled = true;
                    luigi_LevelRandomizationSeedMax.Enabled = true;
                }
            }

        }

        private void luigi_MaxFPS_CheckedChanged(object sender, EventArgs e)
        {
            luigiParameters.maximizeFps = (sender as CheckBox).Checked;
            luigiParameters.UpdateString(true);
        }

        private void luigi_VictorySound_CheckedChanged(object sender, EventArgs e)
        {
            luigiParameters.playSoundOnWin = (sender as CheckBox).Checked;
            luigiParameters.UpdateString(true);
        }

        private void luigi_IncreaseDifficulty_CheckedChanged(object sender, EventArgs e)
        {
            luigi_IncreaseDifficultyAmount.Enabled = (sender as CheckBox).Checked;
        }

        private void luigi_IncreaseLength_CheckedChanged(object sender, EventArgs e)
        {
            luigi_IncreaseLengthAmount.Enabled = (sender as CheckBox).Checked;
        }
	}

    public static class Extensions
    {
        private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);

        private delegate void GetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);

        public static void SetControlPropertyThreadSafe(this Control control, string propertyName, object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate(SetControlPropertyThreadSafe), new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(propertyName, System.Reflection.BindingFlags.SetProperty, null, control, new object[] { propertyValue });
            }
        }

        public static object GetControlPropertyThreadSafe(this Control control, string propertyName)
        {
            return control.GetType().InvokeMember(propertyName, System.Reflection.BindingFlags.GetProperty, null, control, null);
        }
    }
}
