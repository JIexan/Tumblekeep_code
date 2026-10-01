using UnityEngine;
using DG.Tweening;

public class MoneySpellEffect : TimedSpellEffect
{
    [SerializeField] private ParticleSystem mainParticleSystem;
    [SerializeField] private ParticleSystem childParticleSystem;
    [SerializeField] private Material effectMaterial;
    [SerializeField] private string materialPropertyName = "_Alpha";
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    public void StartEffect(float effectDuration) {
        StartTimer(effectDuration);

        // Плавно включаем виньетку: свойство материала от 0 до 1
        effectMaterial.SetFloat(materialPropertyName, 0f);
        DOTween.To(() => effectMaterial.GetFloat(materialPropertyName),
            x => effectMaterial.SetFloat(materialPropertyName, x), 1f, fadeInDuration);

        mainParticleSystem.gameObject.SetActive(true);
        mainParticleSystem.Play();
    }

    protected override void StopEffect() {
        // Плавно выключаем виньетку: от 1 до 0
        DOTween.To(() => effectMaterial.GetFloat(materialPropertyName),
            x => effectMaterial.SetFloat(materialPropertyName, x), 0f, fadeOutDuration);

        // Останавливаем эмиссию, но даём уже вылетевшим частицам дожить
        childParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Сбрасываем множитель монет
        CoinsManager.Instance.ResetMultiplier();

        // Отключаем объект, когда частицы умрут
        StartCoroutine(WaitForParticlesDeath());
    }

    private System.Collections.IEnumerator WaitForParticlesDeath() {
        while (childParticleSystem.IsAlive(true)) {
            yield return null;
        }
        mainParticleSystem.gameObject.SetActive(false);
    }
}
