namespace NeuralNetworkNET.APIs.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWeightedLayer
    {
        /// <summary>
        /// 
        /// </summary>
        float[] Biases { get; }
        /// <summary>
        /// 
        /// </summary>
        float[] Weights { get; }
    }
}