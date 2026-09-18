using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
	public Vector2 min;

	public Vector2 max;

	public GameObject prefab;

	public Vector2Int numberOfObjects;

	internal GridObject[,] spawnedObjects;

	public static GridVisualizer instance;

	private void Start()
	{
		instance = this;
		SpawnGrid();
	}

	public void BulletCall(Vector2 worldSpacePosition)
	{
		Vector2Int gridPos = WorldToGridSpace(worldSpacePosition);
		float power = Vector2.Distance(worldSpacePosition, GridToWorldSpace(gridPos));
		spawnedObjects[gridPos.x, gridPos.y].BopCall(power);
	}

	public Vector2 GridToWorldSpace(int x, int y)
	{
		return GridToWorldSpace(new Vector2Int(x, y));
	}

	public Vector2 GridToWorldSpace(Vector2Int gridPos)
	{
		return new Vector2(Mathf.Lerp(min.x, max.x, (float)gridPos.x / (float)numberOfObjects.x), Mathf.Lerp(min.y, max.y, (float)gridPos.y / (float)numberOfObjects.y));
	}

	public Vector2Int WorldToGridSpace(Vector2 pos)
	{
		return new Vector2Int((int)(Mathf.InverseLerp(min.x, max.x, pos.x) * (float)numberOfObjects.x), (int)(Mathf.InverseLerp(min.y, max.y, pos.y) * (float)numberOfObjects.y));
	}

	private void SpawnGrid()
	{
		spawnedObjects = new GridObject[numberOfObjects.x, numberOfObjects.y];
		for (int i = 0; i < numberOfObjects.x; i++)
		{
			for (int j = 0; j < numberOfObjects.y; j++)
			{
				spawnedObjects[i, j] = Object.Instantiate(prefab, GridToWorldSpace(i, j), Quaternion.identity).GetComponent<GridObject>();
			}
		}
	}
}
