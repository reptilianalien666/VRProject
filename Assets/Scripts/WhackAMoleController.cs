using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class WhackAMoleController : MonoBehaviour
{
    public List<GameObject> moles; // List of mole game objects
    public float minPopUpInterval = 1f; // Minimum interval between mole pop-ups
    public float maxPopUpInterval = 3f; // Maximum interval between mole pop-ups
    public float popUpHeight = 0.5f; // Height at which mole pops up
    public float popDownSpeed = 5f; // Speed at which mole moves down
    public AudioClip popUpSound; // Sound effect for mole pop-up
    public TextMeshProUGUI scoreText; // TextMeshPro text for displaying score
    private int score = 0; // Player's score

    private void Start()
    {
        StartCoroutine(PopUpAndDownMoles());
    }

    private IEnumerator PopUpAndDownMoles()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minPopUpInterval, maxPopUpInterval));

            // Choose a random mole to pop up
            GameObject mole = moles[Random.Range(0, moles.Count)];

            // Play pop-up sound effect
            AudioSource.PlayClipAtPoint(popUpSound, mole.transform.position);

            // Get the initial and target positions
            Vector3 startPos = mole.transform.position;
            Vector3 endPos = startPos + Vector3.up * popUpHeight;

            // Move mole up
            mole.SetActive(true);
            float elapsedTime = 0f;
            while (elapsedTime < 0.5f) // Adjust this duration as needed
            {
                elapsedTime += Time.deltaTime;
                mole.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / 0.5f);
                yield return null;
            }

            // Wait for pop-up duration
            yield return new WaitForSeconds(Random.Range(1f, 2f)); // Adjust these ranges as needed

            // Move mole down
            elapsedTime = 0f;
            while (elapsedTime < 0.5f) // Adjust this duration as needed
            {
                elapsedTime += Time.deltaTime;
                mole.transform.position = Vector3.Lerp(endPos, startPos, elapsedTime / 0.5f);
                yield return null;
            }

            // Disable mole
            mole.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Mole"))
        {
            // Increase the score
            score++;
            // Update the score text
            scoreText.text = "Score: " + score;
            // Move the mole down
            other.transform.parent.gameObject.SetActive(false);
        }
    }
}