const { env } = require('process');

//const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
//  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:49682';
const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:31381/api';

const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
   ],
    target: target,
    secure: false,
    bypass: function (req, res, opts) {
      req.headers["x-api-version"] = "1.1";
    }
  },
  {
    context: [
      "/mainservice",
   ],
    target: target,
    secure: false,
    bypass: function (req, res, opts) {
      req.headers["x-api-version"] = "3.0";
    }
  }
]

module.exports = PROXY_CONFIG;
