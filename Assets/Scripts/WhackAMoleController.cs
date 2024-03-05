using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class WhackAMoleController : MonoBehaviour
{
    public List<GameObject> moles; // List of mole game objects
    public float minPopUpInterval = 1f; // Minimum interval between mole pop-ups
    public float maxPopUpInterval = 3f; // Maximum interval between mole pop-ups
    public float minPopUpTime = 1f; // Minimum time a mole stays popped up
    public float maxPopUpTime = 2f; // Maximum time a mole stays popped up
    public float popUpHeight = 0.5f; // Height at which mole pops up
    public float popDownSpeed = 5f; // Speed at which mole moves down
    public AudioClip popUpSound; // Sound effect for mole pop-up
    public TextMeshProUGUI scoreText; // TextMeshPro text for displaying score
    private int score = 0; // Player's score

    private void Start()
    {
        StartCoroutine(PopUpMoles());
    }

    private void Update()
    {

    }

    IEnumerator PopUpMoles()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minPopUpInterval, maxPopUpInterval));

            // Choose a random mole to pop up
            GameObject mole = moles[Random.Range(0, moles.Count)];

            // Play pop-up sound effect
            AudioSource.PlayClipAtPoint(popUpSound, mole.transform.position);

            // Get the initial position of the mole
            Vector3 startPos = mole.transform.position;

            // Move mole up
            mole.SetActive(true);
            StartCoroutine(MoveMole(mole.transform, startPos, startPos + Vector3.up * popUpHeight, Random.Range(minPopUpTime, maxPopUpTime)));

            yield return null;
        }
    }

    IEnumerator MoveMole(Transform moleTransform, Vector3 startPos, Vector3 endPos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            moleTransform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        // Wait for a short delay before moving the mole down
        yield return new WaitForSeconds(0.5f);

        // Move mole down
        StartCoroutine(MoveMoleDown(moleTransform, moleTransform.position, startPos, popDownSpeed));
    }

    IEnumerator MoveMoleDown(Transform moleTransform, Vector3 startPos, Vector3 endPos, float speed)
    {
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / speed;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            moleTransform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }
        moleTransform.position = endPos;
        moleTransform.gameObject.SetActive(false);
        yield return null;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision detected!");
        if (other.CompareTag("Mole"))
        {
            Debug.Log("Mole hit by hammer!");
            // Increase the score
            score++;
            // Update the score text
            scoreText.text = "Score: " + score;
            // Move the mole down
            other.transform.parent.gameObject.SetActive(false);
        }
    }
}
