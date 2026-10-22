Settings now save changed values at 60-second intervals and flush pending changes on logout, normal shutdown and unload. Three validated backup generations support recovery from damaged or missing configuration files. Repeated failures stop after three attempts and offer an explicit retry.

Discord operations now share one tracked, cancellable worker which is drained before unloading. PNG encoding runs on that worker; screenshots remain in memory. Existing saved settings and language choices are preserved.

Validation: Release build passed with zero warnings and errors. All 40 persistence/lifecycle assertions and localization, legacy-config, canonical-tab and PNG cancellation smoke tests passed. The package includes no user configuration or test data. Native game rendering, real Discord posting and real power-loss behavior remain unverified.

SHA256: E11690F4C69C4BA1E44E4E71D3F927DF67A05445F77CC563D144A5210EB0E095
