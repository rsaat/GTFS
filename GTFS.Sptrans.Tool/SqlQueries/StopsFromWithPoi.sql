SELECT st.trip_id,t.shape_id,st.stop_id, st.stop_sequence,
st.departure_time,
st.shape_dist_traveled,
s.stop_name,s.stop_lat,
s.stop_lon,s.stop_geohash,p.poi_name,
st.pickup_type,
st.drop_off_type
from  stop_time st 
INNER JOIN trip t ON t.id = st.trip_id 
INNER JOIN stop s ON s.id = st.stop_id
LEFT JOIN poi_stop ps ON ps.stop_id_fk = s.id 
LEFT JOIN poi p ON p.poi_id = ps.poi_id_fk
WHERE ((1=0) OR st.trip_id = "106A-10-0") AND (st.stop_sequence>0)
ORDER BY  st.trip_id, st.stop_sequence