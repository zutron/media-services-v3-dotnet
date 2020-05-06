﻿namespace media_services_high_availability_shared.Services
{
    using media_services_high_availability_shared.Helpers;
    using media_services_high_availability_shared.Models;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MediaServiceInstanceHealthStorageService : IMediaServiceInstanceHealthStorageService
    {
        private readonly ILogger logger;
        private static DateTime minDateTimeForTableStorage = new DateTime(1900, 1, 1);
        private readonly TableStorageService tableStorageService;

        public MediaServiceInstanceHealthStorageService(TableStorageService tableStorageService, ILogger logger)
        {
            this.tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MediaServiceInstanceHealthModel> CreateOrUpdateAsync(MediaServiceInstanceHealthModel mediaServiceInstanceHealthModel)
        {
            if (mediaServiceInstanceHealthModel == null)
            {
                throw new ArgumentNullException(nameof(mediaServiceInstanceHealthModel));
            }

            var verifiedModel = new MediaServiceInstanceHealthModel
            {
                IsHealthy = mediaServiceInstanceHealthModel.IsHealthy,
                LastFailedJob = VerifyMinValue(mediaServiceInstanceHealthModel.LastFailedJob),
                LastSubmittedJob = VerifyMinValue(mediaServiceInstanceHealthModel.LastSubmittedJob),
                LastSuccessfulJob = VerifyMinValue(mediaServiceInstanceHealthModel.LastSuccessfulJob),
                LastUpdated = VerifyMinValue(mediaServiceInstanceHealthModel.LastUpdated),
                MediaServiceAccountName = mediaServiceInstanceHealthModel.MediaServiceAccountName
            };

            var mediaServiceInstanceHealthResult = await this.tableStorageService.CreateOrUpdateAsync(new MediaServiceInstanceHealthModelTableEntity(verifiedModel)).ConfigureAwait(false);

            var mediaServiceInstanceHealthModelResult = mediaServiceInstanceHealthResult.GetMediaServiceInstanceHealthModel();
            this.logger.LogInformation($"MediaServiceInstanceHealthStorageService::CreateOrUpdateAsync completed: mediaServiceInstanceHealthModelResult={LogHelper.FormatObjectForLog(mediaServiceInstanceHealthModelResult)}");

            return mediaServiceInstanceHealthModelResult;
        }

        public async Task<MediaServiceInstanceHealthModel> GetAsync(string mediaServiceName)
        {
            var model = await this.tableStorageService.GetAsync<MediaServiceInstanceHealthModelTableEntity>(mediaServiceName, MediaServiceInstanceHealthModelTableEntity.DefaultRowKeyValue).ConfigureAwait(false);
            return model.GetMediaServiceInstanceHealthModel();
        }

        public async Task<IEnumerable<MediaServiceInstanceHealthModel>> ListAsync()
        {
            return (await this.tableStorageService.ListAsync<MediaServiceInstanceHealthModelTableEntity>().ConfigureAwait(false)).Select(i => i.GetMediaServiceInstanceHealthModel());
        }

        public async Task<MediaServiceInstanceHealthModel> UpdateProcessedJobStateAsync(string mediaServiceName, bool isJobCompletedSuccessfully, DateTime eventDateTime)
        {
            var getResult = await this.tableStorageService.GetAsync<MediaServiceInstanceHealthModelTableEntity>(mediaServiceName, MediaServiceInstanceHealthModelTableEntity.DefaultRowKeyValue).ConfigureAwait(false);

            eventDateTime = VerifyMinValue(eventDateTime);
            getResult.LastUpdated = eventDateTime;

            if (isJobCompletedSuccessfully)
            {
                getResult.LastSuccessfulJob = eventDateTime;
            }
            else
            {
                getResult.LastFailedJob = eventDateTime;
            }

            var mergeResult = await this.tableStorageService.MergeAsync(getResult).ConfigureAwait(false);
            return mergeResult.GetMediaServiceInstanceHealthModel();
        }

        public async Task<MediaServiceInstanceHealthModel> UpdateSubmittedJobStateAsync(string mediaServiceName, DateTime eventDateTime)
        {
            var getResult = await this.tableStorageService.GetAsync<MediaServiceInstanceHealthModelTableEntity>(mediaServiceName, MediaServiceInstanceHealthModelTableEntity.DefaultRowKeyValue).ConfigureAwait(false);

            eventDateTime = VerifyMinValue(eventDateTime);
            getResult.LastUpdated = eventDateTime;
            getResult.LastSubmittedJob = eventDateTime;

            var mergeResult = await this.tableStorageService.MergeAsync(getResult).ConfigureAwait(false);
            return mergeResult.GetMediaServiceInstanceHealthModel();
        }

        public async Task<MediaServiceInstanceHealthModel> UpdateHealthStateAsync(string mediaServiceName, bool isHealthy, DateTime eventDateTime)
        {
            var getResult = await this.tableStorageService.GetAsync<MediaServiceInstanceHealthModelTableEntity>(mediaServiceName, MediaServiceInstanceHealthModelTableEntity.DefaultRowKeyValue).ConfigureAwait(false);

            eventDateTime = VerifyMinValue(eventDateTime);
            getResult.LastUpdated = eventDateTime;
            getResult.IsHealthy = isHealthy;

            var mergeResult = await this.tableStorageService.MergeAsync(getResult).ConfigureAwait(false);
            return mergeResult.GetMediaServiceInstanceHealthModel();
        }

        private static DateTime VerifyMinValue(DateTime dateTime)
        {
            return dateTime > minDateTimeForTableStorage ? dateTime : minDateTimeForTableStorage;
        }
    }
}