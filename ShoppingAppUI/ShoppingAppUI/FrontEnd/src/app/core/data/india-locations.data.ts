export interface LocationData {
  [state: string]: {
    [district: string]: string[];
  };
}

export const INDIA_LOCATIONS: LocationData = {
  'Tamil Nadu': {
    'Chennai': ['Ambattur', 'Anna Nagar', 'Adyar', 'Velachery', 'Tambaram', 'Porur', 'Perambur', 'Tondiarpet'],
    'Coimbatore': ['Coimbatore City', 'Pollachi', 'Mettupalayam', 'Tiruppur', 'Udumalaipettai'],
    'Madurai': ['Madurai City', 'Melur', 'Thirumangalam', 'Usilampatti', 'Vadipatti'],
    'Salem': ['Salem City', 'Attur', 'Mettur', 'Omalur', 'Sankari'],
    'Tiruchirappalli': ['Trichy City', 'Lalgudi', 'Manachanallur', 'Musiri', 'Srirangam'],
    'Tirunelveli': ['Tirunelveli City', 'Ambasamudram', 'Nanguneri', 'Palayamkottai', 'Tenkasi'],
    'Vellore': ['Vellore City', 'Ambur', 'Gudiyatham', 'Ranipet', 'Vaniyambadi'],
    'Erode': ['Erode City', 'Bhavani', 'Gobichettipalayam', 'Perundurai', 'Sathyamangalam'],
    'Thanjavur': ['Thanjavur City', 'Kumbakonam', 'Papanasam', 'Pattukkottai', 'Peravurani'],
    'Dindigul': ['Dindigul City', 'Kodaikanal', 'Natham', 'Nilakottai', 'Palani','Oddanchatram']
  },
  'Kerala': {
    'Thiruvananthapuram': ['Thiruvananthapuram City', 'Attingal', 'Nedumangad', 'Neyyattinkara', 'Varkala'],
    'Ernakulam': ['Kochi', 'Aluva', 'Angamaly', 'Muvattupuzha', 'Perumbavoor'],
    'Kozhikode': ['Kozhikode City', 'Koyilandy', 'Ramanattukara', 'Vadakara', 'Feroke'],
    'Thrissur': ['Thrissur City', 'Chalakudy', 'Guruvayur', 'Irinjalakuda', 'Kodungallur'],
    'Palakkad': ['Palakkad City', 'Chittur', 'Mannarkkad', 'Ottapalam', 'Shoranur'],
    'Malappuram': ['Malappuram City', 'Manjeri', 'Perinthalmanna', 'Tirur', 'Tiruvali'],
    'Kannur': ['Kannur City', 'Iritty', 'Payyanur', 'Taliparamba', 'Mattannur'],
    'Kollam': ['Kollam City', 'Karunagappally', 'Kottarakkara', 'Punalur', 'Paravur'],
    'Kottayam': ['Kottayam City', 'Changanacherry', 'Ettumanoor', 'Pala', 'Vaikom'],
    'Alappuzha': ['Alappuzha City', 'Chengannur', 'Cherthala', 'Kayamkulam', 'Mavelikkara']
  },
  'Karnataka': {
    'Bengaluru Urban': ['Bengaluru City', 'Anekal', 'Devanahalli', 'Doddaballapura', 'Hoskote'],
    'Mysuru': ['Mysuru City', 'Hunsur', 'Krishnarajanagara', 'Nanjangud', 'Periyapatna'],
    'Mangaluru': ['Mangaluru City', 'Bantval', 'Belthangady', 'Puttur', 'Sullia'],
    'Hubballi-Dharwad': ['Hubballi', 'Dharwad', 'Kundgol', 'Kalghatgi', 'Navalgund'],
    'Belagavi': ['Belagavi City', 'Athani', 'Bailhongal', 'Chikodi', 'Gokak'],
    'Kalaburagi': ['Kalaburagi City', 'Afzalpur', 'Aland', 'Chincholi', 'Jewargi'],
    'Ballari': ['Ballari City', 'Hagaribommanahalli', 'Hospet', 'Kudligi', 'Sandur'],
    'Shivamogga': ['Shivamogga City', 'Bhadravati', 'Sagar', 'Shikaripura', 'Soraba'],
    'Tumakuru': ['Tumakuru City', 'Chikkanayakanahalli', 'Gubbi', 'Madhugiri', 'Tiptur'],
    'Vijayapura': ['Vijayapura City', 'Basavana Bagewadi', 'Indi', 'Muddebihal', 'Sindagi']
  },
  'Maharashtra': {
    'Mumbai City': ['Colaba', 'Bandra', 'Andheri', 'Borivali', 'Kurla'],
    'Mumbai Suburban': ['Thane', 'Mulund', 'Ghatkopar', 'Vikhroli', 'Powai'],
    'Pune': ['Pune City', 'Pimpri-Chinchwad', 'Baramati', 'Indapur', 'Shirur'],
    'Nagpur': ['Nagpur City', 'Hingna', 'Kamptee', 'Katol', 'Narkhed'],
    'Nashik': ['Nashik City', 'Igatpuri', 'Malegaon', 'Niphad', 'Sinnar'],
    'Aurangabad': ['Aurangabad City', 'Gangapur', 'Kannad', 'Paithan', 'Vaijapur'],
    'Solapur': ['Solapur City', 'Akkalkot', 'Barshi', 'Mangalvedhe', 'Pandharpur'],
    'Kolhapur': ['Kolhapur City', 'Gadhinglaj', 'Hatkanangle', 'Kagal', 'Radhanagari'],
    'Satara': ['Satara City', 'Jaoli', 'Karad', 'Khandala', 'Patan'],
    'Sangli': ['Sangli City', 'Jat', 'Khanapur', 'Miraj', 'Walwa']
  },
  'Andhra Pradesh': {
    'Visakhapatnam': ['Visakhapatnam City', 'Bheemunipatnam', 'Gajuwaka', 'Paderu', 'Yellamanchili'],
    'Vijayawada': ['Vijayawada City', 'Gudivada', 'Jaggayyapeta', 'Nandigama', 'Nuzvid'],
    'Guntur': ['Guntur City', 'Bapatla', 'Narasaraopet', 'Ponnur', 'Tenali'],
    'Tirupati': ['Tirupati City', 'Chandragiri', 'Nagari', 'Puttur', 'Srikalahasti'],
    'Kurnool': ['Kurnool City', 'Adoni', 'Alur', 'Atmakur', 'Nandyal'],
    'Kadapa': ['Kadapa City', 'Badvel', 'Jammalamadugu', 'Proddatur', 'Rajampet'],
    'Nellore': ['Nellore City', 'Atmakur', 'Gudur', 'Kavali', 'Sullurpeta'],
    'Anantapur': ['Anantapur City', 'Dharmavaram', 'Guntakal', 'Hindupur', 'Tadipatri'],
    'Srikakulam': ['Srikakulam City', 'Amadalavalasa', 'Narasannapeta', 'Palakonda', 'Rajam'],
    'East Godavari': ['Kakinada', 'Amalapuram', 'Peddapuram', 'Rajahmundry', 'Ramachandrapuram']
  },
  'Telangana': {
    'Hyderabad': ['Hyderabad City', 'Secunderabad', 'Kukatpally', 'LB Nagar', 'Uppal'],
    'Rangareddy': ['Rangareddy', 'Chevella', 'Maheshwaram', 'Rajendranagar', 'Shadnagar'],
    'Medchal-Malkajgiri': ['Medchal', 'Alwal', 'Balanagar', 'Kompally', 'Quthbullapur'],
    'Warangal Urban': ['Warangal City', 'Hanamkonda', 'Kazipet', 'Parkal', 'Narsampet'],
    'Karimnagar': ['Karimnagar City', 'Huzurabad', 'Jagtial', 'Manthani', 'Sircilla'],
    'Nizamabad': ['Nizamabad City', 'Armoor', 'Banswada', 'Bodhan', 'Kamareddy'],
    'Khammam': ['Khammam City', 'Bhadrachalam', 'Kothagudem', 'Madhira', 'Yellandu'],
    'Nalgonda': ['Nalgonda City', 'Bhongir', 'Devarakonda', 'Miryalaguda', 'Suryapet'],
    'Mahbubnagar': ['Mahbubnagar City', 'Achampet', 'Gadwal', 'Jadcherla', 'Wanaparthy'],
    'Adilabad': ['Adilabad City', 'Asifabad', 'Bellampalli', 'Mancherial', 'Nirmal']
  },
  'Delhi': {
    'Central Delhi': ['Connaught Place', 'Karol Bagh', 'Paharganj', 'Rajinder Nagar', 'Sadar Bazaar'],
    'East Delhi': ['Laxmi Nagar', 'Preet Vihar', 'Shahdara', 'Vivek Vihar', 'Patparganj'],
    'North Delhi': ['Civil Lines', 'Model Town', 'Mukherjee Nagar', 'Rohini', 'Shalimar Bagh'],
    'South Delhi': ['Defence Colony', 'Greater Kailash', 'Hauz Khas', 'Lajpat Nagar', 'Saket'],
    'West Delhi': ['Dwarka', 'Janakpuri', 'Paschim Vihar', 'Punjabi Bagh', 'Tilak Nagar'],
    'North West Delhi': ['Pitampura', 'Ashok Vihar', 'Keshav Puram', 'Mangolpuri', 'Sultanpuri'],
    'South West Delhi': ['Dwarka Sector 10', 'Dwarka Sector 12', 'Najafgarh', 'Palam', 'Uttam Nagar'],
    'New Delhi': ['Chanakyapuri', 'Diplomatic Enclave', 'Lodhi Colony', 'Moti Bagh', 'RK Puram']
  },
  'Uttar Pradesh': {
    'Lucknow': ['Lucknow City', 'Bakshi Ka Talab', 'Chinhat', 'Malihabad', 'Mohanlalganj'],
    'Kanpur Nagar': ['Kanpur City', 'Bilhaur', 'Ghatampur', 'Kalyanpur', 'Shivrajpur'],
    'Agra': ['Agra City', 'Etmadpur', 'Fatehabad', 'Kheragarh', 'Kiraoli'],
    'Varanasi': ['Varanasi City', 'Arajiline', 'Chiraigaon', 'Pindra', 'Sewapuri'],
    'Prayagraj': ['Prayagraj City', 'Bara', 'Handia', 'Karchana', 'Meja'],
    'Ghaziabad': ['Ghaziabad City', 'Hapur', 'Loni', 'Modinagar', 'Muradnagar'],
    'Noida (Gautam Buddha Nagar)': ['Noida', 'Greater Noida', 'Dadri', 'Jewar', 'Rabupura'],
    'Meerut': ['Meerut City', 'Hapur', 'Kithore', 'Mawana', 'Sardhana'],
    'Mathura': ['Mathura City', 'Chhata', 'Govardhan', 'Mant', 'Vrindavan'],
    'Bareilly': ['Bareilly City', 'Aonla', 'Baheri', 'Faridpur', 'Nawabganj']
  },
  'West Bengal': {
    'Kolkata': ['Kolkata City', 'Alipore', 'Behala', 'Dum Dum', 'Salt Lake'],
    'North 24 Parganas': ['Barasat', 'Barrackpore', 'Basirhat', 'Bongaon', 'Habra'],
    'South 24 Parganas': ['Alipurduar', 'Baruipur', 'Budge Budge', 'Diamond Harbour', 'Kakdwip'],
    'Howrah': ['Howrah City', 'Amta', 'Bagnan', 'Jagatballavpur', 'Uluberia'],
    'Hooghly': ['Chinsurah', 'Arambagh', 'Chandannagar', 'Serampore', 'Tarakeswar'],
    'Bardhaman': ['Bardhaman City', 'Asansol', 'Durgapur', 'Kalna', 'Katwa'],
    'Murshidabad': ['Berhampore', 'Domkal', 'Jangipur', 'Kandi', 'Lalbagh'],
    'Nadia': ['Krishnanagar', 'Chakdaha', 'Kalyani', 'Nabadwip', 'Ranaghat'],
    'Darjeeling': ['Darjeeling City', 'Kalimpong', 'Kurseong', 'Mirik', 'Siliguri'],
    'Malda': ['Malda City', 'English Bazar', 'Gazole', 'Habibpur', 'Old Malda']
  },
  'Rajasthan': {
    'Jaipur': ['Jaipur City', 'Amber', 'Bassi', 'Chaksu', 'Sanganer'],
    'Jodhpur': ['Jodhpur City', 'Bhopalgarh', 'Bilara', 'Luni', 'Osian'],
    'Udaipur': ['Udaipur City', 'Girwa', 'Kherwara', 'Mavli', 'Vallabhnagar'],
    'Kota': ['Kota City', 'Baran', 'Itawa', 'Ladpura', 'Sangod'],
    'Ajmer': ['Ajmer City', 'Beawar', 'Kekri', 'Kishangarh', 'Nasirabad'],
    'Bikaner': ['Bikaner City', 'Chhatargarh', 'Kolayat', 'Lunkaransar', 'Nokha'],
    'Alwar': ['Alwar City', 'Behror', 'Kishangarh Bas', 'Laxmangarh', 'Rajgarh'],
    'Bharatpur': ['Bharatpur City', 'Bayana', 'Deeg', 'Kaman', 'Nagar'],
    'Sikar': ['Sikar City', 'Danta Ramgarh', 'Fatehpur', 'Laxmangarh', 'Neem Ka Thana'],
    'Pali': ['Pali City', 'Bali', 'Desuri', 'Marwar Junction', 'Sumerpur']
  },
  'Gujarat': {
    'Ahmedabad': ['Ahmedabad City', 'Daskroi', 'Detroj-Rampura', 'Dhandhuka', 'Sanand'],
    'Surat': ['Surat City', 'Bardoli', 'Kamrej', 'Mangrol', 'Olpad'],
    'Vadodara': ['Vadodara City', 'Dabhoi', 'Karjan', 'Padra', 'Savli'],
    'Rajkot': ['Rajkot City', 'Gondal', 'Jasdan', 'Jetpur', 'Wankaner'],
    'Bhavnagar': ['Bhavnagar City', 'Gariadhar', 'Ghogha', 'Mahuva', 'Talaja'],
    'Jamnagar': ['Jamnagar City', 'Dhrol', 'Jodiya', 'Jodia', 'Kalavad'],
    'Gandhinagar': ['Gandhinagar City', 'Dehgam', 'Kalol', 'Mansa', 'Vijapur'],
    'Anand': ['Anand City', 'Anklav', 'Borsad', 'Khambhat', 'Petlad'],
    'Mehsana': ['Mehsana City', 'Becharaji', 'Kadi', 'Unjha', 'Visnagar'],
    'Kutch': ['Bhuj', 'Anjar', 'Gandhidham', 'Mandvi', 'Mundra']
  },
  'Madhya Pradesh': {
    'Bhopal': ['Bhopal City', 'Berasia', 'Huzur', 'Phanda', 'Sehore'],
    'Indore': ['Indore City', 'Depalpur', 'Mhow', 'Sanwer', 'Simrol'],
    'Jabalpur': ['Jabalpur City', 'Katni', 'Kundam', 'Patan', 'Sihora'],
    'Gwalior': ['Gwalior City', 'Bhitarwar', 'Dabra', 'Morar', 'Pichhore'],
    'Ujjain': ['Ujjain City', 'Ghattia', 'Khachrod', 'Mahidpur', 'Nagda'],
    'Rewa': ['Rewa City', 'Gurh', 'Hanumana', 'Mauganj', 'Teonthar'],
    'Sagar': ['Sagar City', 'Banda', 'Bina', 'Khurai', 'Rahatgarh'],
    'Satna': ['Satna City', 'Amarpatan', 'Maihar', 'Nagod', 'Rampur Baghelan'],
    'Dewas': ['Dewas City', 'Bagli', 'Kannod', 'Khategaon', 'Sonkatch'],
    'Ratlam': ['Ratlam City', 'Alot', 'Jaora', 'Sailana', 'Tal']
  },
  'Punjab': {
    'Ludhiana': ['Ludhiana City', 'Dehlon', 'Jagraon', 'Khanna', 'Samrala'],
    'Amritsar': ['Amritsar City', 'Ajnala', 'Attari', 'Baba Bakala', 'Majitha'],
    'Jalandhar': ['Jalandhar City', 'Adampur', 'Bhogpur', 'Nakodar', 'Phillaur'],
    'Patiala': ['Patiala City', 'Nabha', 'Rajpura', 'Samana', 'Sangrur'],
    'Mohali (SAS Nagar)': ['Mohali City', 'Dera Bassi', 'Kharar', 'Kurali', 'Morinda'],
    'Bathinda': ['Bathinda City', 'Goniana', 'Phul', 'Rampura Phul', 'Talwandi Sabo'],
    'Hoshiarpur': ['Hoshiarpur City', 'Dasuya', 'Garhshankar', 'Mukerian', 'Tanda'],
    'Gurdaspur': ['Gurdaspur City', 'Batala', 'Dina Nagar', 'Dhariwal', 'Pathankot'],
    'Firozpur': ['Firozpur City', 'Fazilka', 'Jalalabad', 'Mamdot', 'Zira'],
    'Kapurthala': ['Kapurthala City', 'Phagwara', 'Sultanpur Lodhi', 'Dhilwan', 'Nadala']
  },
  'Haryana': {
    'Gurugram': ['Gurugram City', 'Farrukhnagar', 'Manesar', 'Pataudi', 'Sohna'],
    'Faridabad': ['Faridabad City', 'Ballabhgarh', 'Badkhal', 'Palwal', 'Tigaon'],
    'Ambala': ['Ambala City', 'Ambala Cantonment', 'Barara', 'Mullana', 'Naraingarh'],
    'Hisar': ['Hisar City', 'Adampur', 'Agroha', 'Barwala', 'Hansi'],
    'Rohtak': ['Rohtak City', 'Asthal Bohar', 'Kalanaur', 'Lakhan Majra', 'Sampla'],
    'Karnal': ['Karnal City', 'Assandh', 'Gharaunda', 'Indri', 'Nilokheri'],
    'Panipat': ['Panipat City', 'Israna', 'Madlauda', 'Samalkha', 'Sanoli'],
    'Sonipat': ['Sonipat City', 'Ganaur', 'Gohana', 'Kharkhoda', 'Rai'],
    'Yamunanagar': ['Yamunanagar City', 'Bilaspur', 'Chhachhrauli', 'Jagadhri', 'Radaur'],
    'Panchkula': ['Panchkula City', 'Barwala', 'Morni', 'Pinjore', 'Raipur Rani']
  }
};

export const INDIA_STATES = Object.keys(INDIA_LOCATIONS).sort();

export function getDistricts(state: string): string[] {
  return state ? Object.keys(INDIA_LOCATIONS[state] || {}).sort() : [];
}

export function getCities(state: string, district: string): string[] {
  return (state && district) ? (INDIA_LOCATIONS[state]?.[district] || []) : [];
}
