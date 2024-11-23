using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class TextAnim : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private AudioClip popSound;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float popScale = 1.5f;
    [SerializeField] private float popDuration = 0.1f;
    [SerializeField] private Animator animator;

    public event Action OnTypewriterComplete;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void StartTypewriterEffect()
    {
        string originalText = textMesh.text;
        textMesh.text = "";
        textMesh.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(TypewriterCoroutine(originalText));
    }

    private IEnumerator TypewriterCoroutine(string originalText)
    {
        textMesh.text = "";

        for (int i = 0; i < originalText.Length; i++)
        {
            textMesh.text += originalText[i];
            textMesh.ForceMeshUpdate();
            TMP_CharacterInfo charInfo = textMesh.textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;
            if (popSound != null)
            {
                audioSource.PlayOneShot(popSound);
            }
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] originalVertices = new Vector3[4];
            Vector3 centerPoint = Vector3.zero;
            for (int j = 0; j < 4; j++)
            {
                originalVertices[j] = textMesh.textInfo.meshInfo[materialIndex].vertices[vertexIndex + j];
                centerPoint += originalVertices[j];
            }
            centerPoint /= 4f;
            float startTime = Time.time;
            while (Time.time < startTime + popDuration)
            {
                float progress = (Time.time - startTime) / popDuration;
                float scale = 1f + (popScale - 1f) * (1f - progress);
                for (int j = 0; j < 4; j++)
                {
                    Vector3 vertex = originalVertices[j];
                    textMesh.textInfo.meshInfo[materialIndex].vertices[vertexIndex + j] =
                        centerPoint + (vertex - centerPoint) * scale;
                }
                textMesh.UpdateVertexData();
                yield return null;
            }
            for (int j = 0; j < 4; j++)
            {
                textMesh.textInfo.meshInfo[materialIndex].vertices[vertexIndex + j] = originalVertices[j];
            }
            textMesh.UpdateVertexData();
            yield return new WaitForSeconds(typingSpeed);
        }
        OnTypewriterComplete?.Invoke();
        TypewriterCompleted();
    }
    private void TypewriterCompleted()
    {
        if (animator != null)
        {
            animator.SetTrigger("showControls");
        }
    }
}