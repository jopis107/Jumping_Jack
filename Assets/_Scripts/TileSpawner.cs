using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;


namespace JumpingJack{
    public class TileSpawner : MonoBehaviour{
        [SerializeField]
        private int tileStartCount = 10; //how many straight tiles will be at the beggining of the game
        [SerializeField]
        private int minimumStraightTiles = 3; //minimum number of tiles between the turns
        [SerializeField]
        private int maximumStraightTiles = 15; //maximum number of tiles between the turns
        [SerializeField]
        private GameObject startingTile;
        // TileSpawner.cs

        [SerializeField]
        private GameObject speedBoostPrefab;
        [SerializeField] [Range(0, 1)]
        private float speedBoostSpawnChance = 0.6f;
        [SerializeField]
        private List<GameObject> turnTiles;
        [SerializeField]
        private List<GameObject> obstacles;

        private Vector3 currentTileLocation = Vector3.zero;
        private Vector3 currentTileDirection = Vector3.forward;
        private GameObject prevTile;

        private List<GameObject> currentTiles; // tracking current tiles
        private List<GameObject> currentObstacles; // tracking current obstacles

        private void Start(){
            currentTiles = new List<GameObject>();
            currentObstacles = new List<GameObject>();

            Random.InitState(System.DateTime.Now.Millisecond); //this gives us very unique random number

            for (int i = 0; i < tileStartCount; i++){
                SpawnTile(startingTile.GetComponent<Tile>()); //spawning the starting tile and false is for obstacles
            }

            SpawnTile(SelectRandomGameObjectFromList(turnTiles).GetComponent<Tile>());

        }

        private void SpawnTile(Tile tile, bool spawnObstacle = false){
            Quaternion newTileRotation = tile.gameObject.transform.rotation * Quaternion.LookRotation(currentTileDirection, Vector3.up);
            prevTile = GameObject.Instantiate(tile.gameObject, currentTileLocation, newTileRotation);
            currentTiles.Add(prevTile);

            if (spawnObstacle) SpawnObstacle();

            if (!spawnObstacle && tile.type == TileType.STRAIGHT){
                if (Random.value < speedBoostSpawnChance){
                    SpawnSpeedBoost();
                }
            }

            if (tile.type == TileType.STRAIGHT)
                currentTileLocation += Vector3.Scale(prevTile.GetComponent<Renderer>().bounds.size, currentTileDirection);
            //offset for spawning the next tile in the right direction so they don't overlap
        }

        private void SpawnSpeedBoost(){
            if (speedBoostPrefab == null) return; // Provjera da ne zaboravimo dodijeliti prefab
            
            Vector3 spawnPos = currentTileLocation;
            spawnPos.y += 1f; // Malo podignemo munju od poda da ne "tone"
            Instantiate(speedBoostPrefab, spawnPos, Quaternion.identity);
        }

        private void DeletePreviousTiles(){
            while (currentTiles.Count != 1){
                GameObject tile = currentTiles[0];
                currentTiles.RemoveAt(0);
                Destroy(tile);
            }

            while (currentObstacles.Count != 0){
                GameObject obstacle = currentObstacles[0];
                currentObstacles.RemoveAt(0);
                Destroy(obstacle);
            }
        }

        public void AddNewDirection(Vector3 direction){
            currentTileDirection = direction;
            DeletePreviousTiles();
            // Calculate the new tile placement scale based on the previous tile's size and the current direction
            Vector3 tilePlacementScale;
            if (prevTile.GetComponent<Tile>().type == TileType.SIDEWAYS){
                tilePlacementScale = Vector3.Scale(prevTile.GetComponent<Renderer>().bounds.size / 2 + (Vector3.one * startingTile.GetComponent<BoxCollider>().size.z / 2), currentTileDirection);
            }
            else{
                tilePlacementScale = Vector3.Scale(prevTile.GetComponent<Renderer>().bounds.size - (Vector3.one * 2) + (Vector3.one * startingTile.GetComponent<BoxCollider>().size.z / 2), currentTileDirection);
            }

            currentTileLocation += tilePlacementScale;

            int currentPathLength = Random.Range(minimumStraightTiles, maximumStraightTiles);
            for (int i = 0; i < currentPathLength; i++){
                SpawnTile(startingTile.GetComponent<Tile>(), (i == 0) ? false : true);
            }
            
            SpawnTile(SelectRandomGameObjectFromList(turnTiles).GetComponent<Tile>(), false);
        }

        private void SpawnObstacle(){
            if (Random.value > 0.3f) return;

            GameObject obstaclePrefab = SelectRandomGameObjectFromList(obstacles);
            Quaternion newObjectRotation = obstaclePrefab.gameObject.transform.rotation * Quaternion.LookRotation(currentTileDirection, Vector3.up);

            if (obstaclePrefab == null) return;
            
            GameObject obstacle = Instantiate(obstaclePrefab, currentTileLocation, newObjectRotation);
            currentObstacles.Add(obstacle);  
        }

        private GameObject SelectRandomGameObjectFromList(List<GameObject> list){
            if (list.Count == 0) return null;

            return list[Random.Range(0, list.Count)];
        }
    }
}