using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Landfall.AI;

public class Population
{
	private List<Genome> m_genomes = new List<Genome>();

	public int Size => m_genomes.Count;

	public Population(int size)
		: this(size, init: true)
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
		return m_genomes[Random.Range(0, m_genomes.Count)];
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
		m_genomes.OrderBy((Genome g) => g.Fitness);
	}
}
