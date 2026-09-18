using System;
using UnityEngine;

namespace Landfall.AI;

public class Genome
{
	private double[] m_genes;

	private float m_fitness;

	public double[] Genes
	{
		get
		{
			return m_genes;
		}
		set
		{
			m_genes = value;
		}
	}

	public float Fitness
	{
		get
		{
			return m_fitness;
		}
		set
		{
			m_fitness = value;
		}
	}

	public int Size => m_genes.Length;

	public Genome()
	{
	}

	public Genome(double[] genes)
	{
		SetGenes(genes);
	}

	public Genome(Genome other)
	{
		m_genes = other.m_genes;
		m_fitness = 0f;
	}

	public void GenerateIndividual()
	{
		NeuralNet neuralNet = new NeuralNet(PlayerAIDavid.topology);
		m_genes = neuralNet.GetWeights().ToArray();
	}

	public void Mutate(float mutationRate)
	{
		for (int i = 0; i < m_genes.Length; i++)
		{
			if (UnityEngine.Random.value < mutationRate)
			{
				System.Random r = new System.Random();
				m_genes[i] += r.NextGaussianDouble();
			}
		}
	}

	public double GetGene(int index)
	{
		return m_genes[index];
	}

	public void SetGene(int index, double value)
	{
		m_genes[index] = value;
		m_fitness = 0f;
	}

	public void SetGenes(double[] genes)
	{
		m_genes = genes;
	}
}
