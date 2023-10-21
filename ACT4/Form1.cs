using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Security.AccessControl;
using System.Threading;
using System.Diagnostics;

namespace ACT4
{
    public partial class Form1 : Form
    {
        int side;
        int n = 6;
        SixState startState;
        SixState currentState;
        int moveCounter;

        //bool stepMove = true;

        int[,] hTable;
        ArrayList bMoves;
        Object chosenMove;

 

        public Form1()
        {
            InitializeComponent();

            side = pictureBox1.Width / n;

            startState = randomSixState();
            currentState = new SixState(startState);

            updateUI();
            label1.Text = "Attacking pairs: " + getAttackingPairs(startState);
        }

        private void updateUI()
        {
            //pictureBox1.Refresh();
            pictureBox2.Refresh();

            //label1.Text = "Attacking pairs: " + getAttackingPairs(startState);
            label3.Text = "Attacking pairs: " + getAttackingPairs(currentState);
            label4.Text = "Moves: " + moveCounter;
            hTable = getHeuristicTableForPossibleMoves(currentState);
            bMoves = getBestMoves(hTable);

            listBox1.Items.Clear();
            foreach (Point move in bMoves)
            {
                listBox1.Items.Add(move);
            }

            if (bMoves.Count > 0)
                chosenMove = chooseMove(bMoves);
            label2.Text = "Chosen move: " + chosenMove;

            label1.Text = "Attacking pairs: " + getAttackingPairs(startState);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // draw squares
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if ((i + j) % 2 == 0)
                    {
                        e.Graphics.FillRectangle(Brushes.Blue, i * side, j * side, side, side);
                    }
                    // draw queens
                    if (j == startState.Y[i])
                        e.Graphics.FillEllipse(Brushes.Fuchsia, i * side, j * side, side, side);
                }
            }
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            // draw squares
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if ((i + j) % 2 == 0)
                    {
                        e.Graphics.FillRectangle(Brushes.Black, i * side, j * side, side, side);
                    }
                    // draw queens
                    if (j == currentState.Y[i])
                        e.Graphics.FillEllipse(Brushes.Fuchsia, i * side, j * side, side, side);
                }
            }
        }

        Random random = new Random();
        bool isSolved = false;

        private SixState randomSixState()
        {
            SixState state = new SixState(random.Next(n),
                                             random.Next(n),
                                             random.Next(n),
                                             random.Next(n),
                                             random.Next(n),
                                             random.Next(n));

            return state;
        }


        List<SixState> population = new List<SixState>();

        private void InitializePopulation(int populationSize)
        {
            population.Clear();
            for (int i = 0; i < populationSize; i++)
            {
                population.Add(randomSixState());
                population[i].FitnessValue = getAttackingPairs(population[i]);
            }
        }

        private SixState RoulletteWheel(List<SixState> population)
        {
            double totalFitness = 0;

            foreach(SixState chromosome in population)
            {
                totalFitness += chromosome.FitnessValue;
            }

            List<double> selectionProbabilities = new List<double>();
            double cumulativeProbability = 0;

            foreach(SixState chromosome in population)
            {
                double probability = chromosome.FitnessValue / totalFitness;
                cumulativeProbability += probability;
                selectionProbabilities.Add(cumulativeProbability);
            }

            for(int i = 0; i<selectionProbabilities.Count; i++)
            {
                if (random.NextDouble() <= selectionProbabilities[i])
                {
                    return population[i];
                }
            }

            return population[random.Next(population.Count)];
        }

        private List<SixState> Crossover(List<SixState> selection)
        {
            
            List<SixState> children = new List<SixState>();
            for(int i = 0; i < selection.Count; i++)
            {
                SixState parent1 = selection[random.Next(1, selection.Count)];
                SixState parent2 = selection[random.Next(1, selection.Count)];

                int crossoverPoint = random.Next(0, 6);
                int[] genes = new int[n];

                Array.Copy(parent1.Y, genes, crossoverPoint);
                Array.Copy(parent2.Y, crossoverPoint, genes, crossoverPoint, n - crossoverPoint);

                SixState child = new SixState(genes);
                children.Add(child);
            }

            return children;
        }

        private void Mutate(SixState chromosome)
        {
            if(random.Next(0,101) <= 10)
            {
                int randomColumn = random.Next(n);
                int randomRow = random.Next(n);
                chromosome.Y[randomColumn] = randomRow;
            }
        }

        int generations = 0;
        int elapsedTime = 0;
        private bool Generate()
        {
            //Population
            InitializePopulation(100);

            while (true)
            {
                //selection by fitness
                List<SixState> selection = new List<SixState>();
                for (int i = 0; i < population.Count; i++)
                {
                    selection.Add(RoulletteWheel(population));
                }

                //crossover
                List<SixState> children = Crossover(selection);


                foreach (SixState child in children)
                {
                    //Mutation
                    Mutate(child);

                    child.FitnessValue = getAttackingPairs(child);

                    currentState = child;

                    if (child.FitnessValue == 0)
                    {
                        isSolved = true;
                        isGenerating = true;
                        timer1.Stop();
                        timer1.Enabled = false;
                        return true;
                    }
                }

                population = children;

                generations++;
            }

            return false;
        }

        private int getAttackingPairs(SixState f)
        {
            int attackers = 0;
            
            for (int rf = 0; rf < n; rf++)
            {
                for (int tar = rf+1; tar < n; tar++)
                {
                    // get horizontal attackers
                    if (f.Y[rf] == f.Y[tar])
                        attackers++;
                }
                for (int tar = rf+1; tar < n; tar++)
                {
                    // get diagonal down attackers
                    if (f.Y[tar] == f.Y[rf] + tar - rf)
                        attackers++;
                }
                for (int tar = rf+1; tar < n; tar++)
                {
                    // get diagonal up attackers
                    if (f.Y[rf] == f.Y[tar] + tar - rf)
                        attackers++;
                }
            }
            
            return attackers;
        }

        private int[,] getHeuristicTableForPossibleMoves(SixState thisState)
        {
            int[,] hStates = new int[n, n];

            for (int i = 0; i < n; i++) // go through the indices
            {
                for (int j = 0; j < n; j++) // replace them with a new value
                {
                    SixState possible = new SixState(thisState);
                    possible.Y[i] = j;
                    hStates[i, j] = getAttackingPairs(possible);
                }
            }

            return hStates;
        }

        private ArrayList getBestMoves(int[,] heuristicTable)
        {
            ArrayList bestMoves = new ArrayList();
            int bestHeuristicValue = heuristicTable[0, 0];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (bestHeuristicValue > heuristicTable[i, j])
                    {
                        bestHeuristicValue = heuristicTable[i, j];
                        bestMoves.Clear();
                        if (currentState.Y[i] != j)
                            bestMoves.Add(new Point(i, j));
                    } else if (bestHeuristicValue == heuristicTable[i,j])
                    {
                        if (currentState.Y[i] != j)
                            bestMoves.Add(new Point(i, j));
                    }
                }
            }
            label5.Text = "Possible Moves (H="+bestHeuristicValue+")";
            return bestMoves;
        }

        private Object chooseMove(ArrayList possibleMoves)
        {
            int arrayLength = possibleMoves.Count;
            Random r = new Random();
            int randomMove = r.Next(arrayLength);

            return possibleMoves[randomMove];
        }

        private void executeMove(Point move)
        {
            for (int i = 0; i < n; i++)
            {
                startState.Y[i] = currentState.Y[i];
            }
            currentState.Y[move.X] = move.Y;
            moveCounter++;

            chosenMove = null;
            updateUI();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (getAttackingPairs(currentState) > 0)
                executeMove((Point)chosenMove);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            elapsedTime = 0;
            attempts.Text = "0";
            generations = 0;

            startState = randomSixState();
            currentState = new SixState(startState);

            moveCounter = 0;

            updateUI();
            pictureBox1.Refresh();
            label1.Text = "Attacking pairs: " + getAttackingPairs(startState);

            isSolved = false;
            isGenerating = false;    
        }

        private void button2_Click(object sender, EventArgs e)
        {
            while (getAttackingPairs(currentState) > 0)
            {
                executeMove((Point)chosenMove);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }


        bool isGenerating = false;
        private void button4_Click(object sender, EventArgs e)
        {  
            if (!isSolved && !isGenerating)
            {
                isGenerating = true;
                label8.Text = "0";

                timer1.Enabled = true;
                timer1.Start();         //start GA elapsed timer label

                timer2.Enabled = true;
                timer2.Start();         //start UI timer for labels

                StartGeneticAlgorithmInThread(); //start GA thread
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            elapsedTime++;
            attempts.Text = elapsedTime.ToString() + "s";
        }


        private void timer2_Tick(object sender, EventArgs e)
        {
            updateUI();
            pictureBox1.Refresh();

            label1.Text = "Attacking pairs: " + getAttackingPairs(startState);
            label8.Text = generations.ToString();
        }


        private delegate void GeneticAlgorithmDelegate();

        private void RunGeneticAlgorithm()
        {
            if(Generate())
            {
                geneticAlgorithmThread.Abort();
                timer2.Stop();
            }
        }

        private Thread geneticAlgorithmThread;
        private void StartGeneticAlgorithmInThread()
        {
            geneticAlgorithmThread = new Thread(new ThreadStart(RunGeneticAlgorithm));
            geneticAlgorithmThread.Start();
        }

    }
}
