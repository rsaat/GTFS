SELECT st.trip_id,r.route_short_name,t.trip_headsign,r.route_long_name,s.stop_geohash
from  stop_time st 
INNER JOIN trip t ON t.id = st.trip_id 
INNER JOIN stop s ON s.id = st.stop_id 
INNER JOIN route r ON r.id=t.route_id
WHERE r.route_short_name LIKE "%1018%"
ORDER BY  t.id, st.stop_sequence