window.CoSpaceConfig = window.CoSpaceConfig || {};
window.CoSpaceConfig.apiBaseUrl = window.CoSpaceConfig.apiBaseUrl || 'http://localhost:5050/api';
const nativeFetch = window.fetch.bind(window);
window.fetch = (resource, options) => {
	if (typeof resource === 'string' && resource.startsWith('http://localhost:5050/api'))
		resource = `${window.CoSpaceConfig.apiBaseUrl}${resource.slice('http://localhost:5050/api'.length)}`;
	return nativeFetch(resource, options);
};