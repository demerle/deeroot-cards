public class MusicGridVisualizer : GridVisualizer
{
	private void Update()
	{
		for (int i = 0; i < numberOfObjects.x; i++)
		{
			for (int j = 0; j < numberOfObjects.y; j++)
			{
				spawnedObjects[i, j].OnSetSize(MusicVisualizerData.Samples[i + j] * 200f);
			}
		}
	}
}
