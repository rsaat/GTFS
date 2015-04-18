SELECT st.trip_id,t.shape_id,st.stop_id, st.stop_sequence,
st.departure_time,
st.shape_dist_traveled,
s.stop_name,s.stop_lat,
s.stop_lon,s.stop_geohash,
st.pickup_type,
st.drop_off_type,
r.route_type
from  stop_time st 
INNER JOIN trip t ON t.id = st.trip_id 
INNER JOIN stop s ON s.id = st.stop_id 
INNER JOIN route r ON r.id=t.route_id
WHERE (r.route_type=1 or r.route_type=2) AND t.direction_id=1
ORDER BY  st.trip_id, st.stop_sequence