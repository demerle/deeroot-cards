using System.Collections.Generic;
using UnityEngine;

namespace Landfall.AI
{
	public class Evolver
	{
		private float m_mutationRate = 0.1f;

		private int m_selectionSize = 5;

		private bool m_elitism = true;

		public Population Evolve(Population population)
		{
			Population population2 = new Population(population.Size, false);
			int num = 0;
			if (m_elitism)
			{
				num = population2.Size / 2;
				population.Sort();
				for (int i = 0; i < num; i++)
				{
					Genome genome = population.GetGenome(i);
					population2.SetGenome(i, genome);
					genome.Fitness = 0f;
				}
				for (int j = 0; j < population2.Size / 2; j++)
				{
					population2.SetGenome(j + num, new Genome(population.GetGenome(j)));
				}
			}
			else
			{
				for (int k = num; k < population2.Size; k++)
				{
					Genome parentA = Selection(population);
					Genome parentB = Selection(population);
					Genome g = Crossover(parentA, parentB);
					population2.SetGenome(k, g);
				}
			}
			for (int l = num; l < population2.Size; l++)
			{
				population2.GetGenome(l).Mutate(m_mutationRate);
			}
			return population2;
		}

		public Genome Crossover(Genome parentA, Genome parentB)
		{
			Genome genome = new Genome();
			int num = Random.Range(0, parentA.Size - 1);
			int num2 = Random.Range(0, parentA.Size - 1);
			for (int i = 0; i < genome.Size; i++)
			{
				if (num < num2 && i > num && i < num2)
				{
					genome.SetGene(i, parentA.GetGene(i));
				}
				else if (num > num2 && (i >= num || i <= num2))
				{
					genome.SetGene(i, parentA.GetGene(i));
				}
				else
				{
					genome.SetGene(i, parentB.GetGene(i));
				}
			}
			return genome;
		}

		public Genome Selection(Population population)
		{
			List<Genome> list = new List<Genome>(m_selectionSize);
			for (int i = 0; i < m_selectionSize; i++)
			{
				list.Add(population.GetGenome(i));
			}
			Genome genome = list[0];
			for (int j = 0; j < m_selectionSize; j++)
			{
				Genome genome2 = population.GetGenome(j);
				if (genome2.Fitness > genome.Fitness)
				{
					genome = genome2;
				}
			}
			return genome;
		}
	}
}
