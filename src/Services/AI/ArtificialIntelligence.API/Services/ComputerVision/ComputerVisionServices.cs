﻿using ArtificialIntelligence.API.Controllers;
using ArtificialIntelligence.API.Services.ComputerVision.Client;
using ArtificialIntelligence.API.Services.ComputerVision.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArtificialIntelligence.API.Services.ComputerVision
{
    public interface IComputerVisionServices
    {
        System.Threading.Tasks.Task<IEnumerable<string>> ClassifyImageAsync(byte[] image, AnalyzeImageModel model);
    }

    public class ComputerVisionServices : IComputerVisionServices
    {
        private readonly Dictionary<AnalyzeImageModel, IClassifier> models;
        private readonly AnalyzeImageModel defaultModel;

        public ComputerVisionServices(IOptionsSnapshot<ArtificialIntelligenceSettings> settings, IHostingEnvironment environment, IModelManagementClient modelManagementClient)
        {
            defaultModel = (AnalyzeImageModel)Enum.Parse(typeof(AnalyzeImageModel), settings.Value.DefaultModel);
            if (defaultModel == AnalyzeImageModel.Default)
                defaultModel = AnalyzeImageModel.TensorFlowInception;

            models = new Dictionary<AnalyzeImageModel, IClassifier>
            {
                { AnalyzeImageModel.TensorFlowInception, new TensorFlowInceptionPrediction(settings, environment) },
                { AnalyzeImageModel.TensorFlowModel, new TensorFlowModelPrediction(settings, environment) },
                { AnalyzeImageModel.ModelManagement, new ModelManagementService (settings, modelManagementClient) }
            };
        }

        /// <summary>
        /// Classify an image, using a model
        /// </summary>
        /// <param name="image">image (jpeg) file to be analyzed</param>
        /// <param name="model">model used for classification</param>
        /// <returns>image related labels</returns>
        public async System.Threading.Tasks.Task<IEnumerable<string>> ClassifyImageAsync(byte[] image, AnalyzeImageModel model)
        {
            if (model == AnalyzeImageModel.Default)
                model = defaultModel;

            if (models.ContainsKey(model))
            {
                var classification = await models[model].ClassifyImageAsync(image);

                return classification.OrderByDescending(c => c.Probability)
                    .Select(c => c.Label);
            }

            return Enumerable.Empty<string>();
        }
    }
}
