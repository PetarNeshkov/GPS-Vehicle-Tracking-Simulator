const target = process.env.GPS_TRACKING_API_URL || 'http://localhost:5078';

module.exports = {
  '/api/**': { target },
  '/hubs/**': { target, ws: true }
};
