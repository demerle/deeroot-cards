using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Landfall.AI
{
	public class Population
	{
		[Serializable]
		[CompilerGenerated]
		private sealed class _003C_003Ec
		{
			public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

			public static Func<Genome, float> _003C_003E9__9_0;

			internal float _003CSort_003Eb__9_0(Genome g)
			{
				return g.Fitness;
			}
		}

		private List<Genome> m_genomes = new List<Genome>();

		public int Size
		{
			get
			{
				return m_genomes.Count;
			}
		}

		public Population(int size)
			: this(size, true)
		{
		}

		public Population(int size, bool init)
		{
			for (int i = 0; i < size; i++)
			{
				m_genomes.Add(null);
			}
			if (init)
			{
				for (int j = 0; j < size; j++)
				{
					Genome genome = new Genome();
					genome.GenerateIndividual();
					m_genomes[j] = genome;
				}
			}
		}

		public Genome GetGenome(int index)
		{
			return m_genomes[index];
		}

		public Genome GetRandomGenome()
		{
			return m_genomes[UnityEngine.Random.Range(0, m_genomes.Count)];
		}

		public void SetGenome(int index, Genome g)
		{
			m_genomes[index] = g;
		}

		public Genome GetFittest()
		{
			Genome genome = m_genomes[0];
			for (int i = 1; i < m_genomes.Count; i++)
			{
				Genome genome2 = m_genomes[i];
				if (genome2.Fitness > genome.Fitness)
				{
					genome = genome2;
				}
			}
			return genome;
		}

		public void Sort()
		{
			m_genomes.OrderBy(_003C_003Ec._003C_003E9__9_0 ?? (_003C_003Ec._003C_003E9__9_0 = _003C_003Ec._003C_003E9._003CSort_003Eb__9_0));
		}
	}
}
