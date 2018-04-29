﻿using NINA.Model.MyFilterWheel;
using NINA.Utility.Mediator;
using System;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [XmlRoot(nameof(FilterWheelSettings))]
    public class FilterWheelSettings {
        private string id = "No_Device";

        [XmlElement(nameof(Id))]
        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private ObserveAllCollection<FilterInfo> filterWheelFilters;

        [XmlElement(nameof(FilterInfo))]
        public ObserveAllCollection<FilterInfo> FilterWheelFilters {
            get {
                if (filterWheelFilters == null) {
                    filterWheelFilters = new ObserveAllCollection<FilterInfo>();
                    /*for (short i = 0; i < 8; i++) {
                        filterWheelFilters.Add(new FilterInfo(Locale.Loc.Instance["LblFilter"] + (i + 1), 0, i, 0));
                    }*/
                }
                return filterWheelFilters;
            }
            set {
                filterWheelFilters = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}