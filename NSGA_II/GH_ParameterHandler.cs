﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace NSGA_II
{

    // Reference for Grasshopper Parameter Handling for optimization: https://github.com/Tomalwo/FrOG
    public class GH_ParameterHandler
    {
        internal static NSGAII_GHComponent gh;
        private static GH_Document ghDoc;

        private static Random random = new Random();

        private static List<GH_NumberSlider> geneInputSliders;

        private AutoResetEvent waitHandle;






        public GH_ParameterHandler(NSGAII_GHComponent _GHComponent)
        {
            gh = _GHComponent;
            ghDoc = gh.OnPingDocument();

            InitializeGeneInputs();

            ghDoc.SolutionEnd += new GH_Document.SolutionEndEventHandler(OnSolutionEnd);
            waitHandle = new AutoResetEvent(false);
            waitHandle.Reset();
        }


        // SetGeneInputs: Initializes list of input gene sliders
        private void InitializeGeneInputs()
        {
            geneInputSliders = new List<GH_NumberSlider>();

            foreach (IGH_Param source in gh.Params.Input[0].Sources)
            {
                GH_NumberSlider slider = source as GH_NumberSlider;   // Add gene pool inputs as well?

                if (slider != null)
                    geneInputSliders.Add(slider);
            }
        }







        // GetSetGeneValues: Sets the gene sliders to a random position and returns the resulting gene values
        internal List<double> GetSetGeneValues()
        {
            var genes = new List<double>();

            foreach (var slider in geneInputSliders)
            {
                slider.Slider.RaiseEvents = false;
                slider.TickValue = random.Next(slider.TickCount + 1);    // +1 (include Max value)
                genes.Add((double)slider.Slider.Value);
                slider.ExpireSolution(false);
                slider.Slider.RaiseEvents = true;
            }

            RecalculateSolution();

            return genes;
        }


        // SetSliders: Sets the gene sliders to the given gene values
        internal void SetSliders(List<double> genes)
        {
            for (int i = 0; i < genes.Count; i++)
            {
                GH_NumberSlider slider = geneInputSliders[i];

                slider.Slider.RaiseEvents = false;
                slider.SetSliderValue((decimal)genes[i]);  
                slider.ExpireSolution(false);
                slider.Slider.RaiseEvents = true;
            }

            RecalculateSolution();
        }


        // ChangeGeneValue: Changes only one slider value for mutation and returns the new gene value
        internal double MutateGeneValue(int index)
        {
            double gene;

            GH_NumberSlider slider = geneInputSliders[index];

            slider.Slider.RaiseEvents = false;
            slider.TickValue = random.Next(slider.TickCount + 1);    // +1 (include Max value)
            gene = (double)slider.Slider.Value;
            slider.ExpireSolution(false);
            slider.Slider.RaiseEvents = true;

            RecalculateSolution();

            return gene;
        }


        // Recalculate: Recalculates Grasshopper solution
        private void RecalculateSolution()
        {
            ghDoc.ScheduleSolution(1);
            waitHandle.WaitOne();

            //ghDoc.NewSolution(false, GH_SolutionMode.CommandLine);
            //while (ghDoc.SolutionState != GH_ProcessStep.PostProcess || ghDoc.SolutionDepth != 0) { }
        }

        private void OnSolutionEnd(object sender, GH_SolutionEventArgs e)
        {
            waitHandle.Set();
        }



        // GetFitnessValues: Collects the Fitness values from the component input
        internal List<double> GetFitnessValues()
        {
            var fitnesses = new List<double>();

            foreach (IGH_Param source in gh.Params.Input[1].Sources)
            {
                foreach (var item in source.VolatileData.AllData(false))
                {
                    double fitness;
                    if (GH_Convert.ToDouble(item, out fitness, GH_Conversion.Both))
                        fitnesses.Add(fitness);
                }
            }

            return fitnesses;
        }



    }
}
