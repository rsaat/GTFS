/*Conexoes com pontos de interesse de uma linha(trip)*/
SELECT st.trip_id,t.shape_id,st.stop_id, st.stop_sequence,
st.shape_dist_traveled,
st.arrival_time,
s.stop_name,s.stop_lat,
s.stop_lon,s.stop_geohash,
p.poi_name,
pc.poi_cat_name,
st.pickup_type,
st.drop_off_type
from  stop_time st 
INNER JOIN trip t ON t.id = st.trip_id 
INNER JOIN stop s ON s.id = st.stop_id
LEFT JOIN poi_stop ps ON ps.stop_id_fk = s.id 
LEFT JOIN poi p ON p.poi_id = ps.poi_id_fk
LEFT JOIN poi_category pc ON p.poi_cat_id_fk = pc.poi_cat_id
WHERE ((1=1) OR st.trip_id = "1764-10-1") AND (st.stop_sequence>0) AND (p.poi_id IS NOT NULL)
ORDER BY  st.trip_id, st.stop_sequence